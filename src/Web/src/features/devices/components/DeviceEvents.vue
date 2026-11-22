<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import {
  deviceKeys,
  devicesApi,
  EVENTS_PAGE_SIZE,
  type DescriptorEvent,
  type DeviceEventItem,
} from '../api'

// Lazy event history for Member+ (D9): loaded only after expanding, with "Starsze" pagination.
const props = defineProps<{
  deviceId: string
  declaredEvents: DescriptorEvent[]
}>()
const { t, d } = useI18n()
const queryClient = useQueryClient()

const expanded = ref(false)
const loadingOlder = ref(false)
const hasMore = ref(false)

const {
  data: events,
  error,
  isPending,
} = useQuery({
  queryKey: computed(() => deviceKeys.events(props.deviceId)),
  queryFn: async () => {
    const first = await devicesApi.events(props.deviceId, 0, EVENTS_PAGE_SIZE)
    hasMore.value = first.length === EVENTS_PAGE_SIZE
    return first
  },
  enabled: expanded,
})

watch(
  () => props.deviceId,
  () => {
    expanded.value = false
    hasMore.value = false
  },
)

function labelFor(name: string): string {
  return props.declaredEvents.find((e) => e.name === name)?.label || name
}

function formatPayload(data: unknown): string | null {
  if (data === null || data === undefined) return null
  if (typeof data === 'object' && data !== null && 'text' in data && typeof (data as { text: unknown }).text === 'string') {
    return (data as { text: string }).text
  }
  return typeof data === 'string' ? data : JSON.stringify(data)
}

async function loadOlder() {
  const current = events.value ?? []
  loadingOlder.value = true
  try {
    const older = await devicesApi.events(props.deviceId, current.length, EVENTS_PAGE_SIZE)
    hasMore.value = older.length === EVENTS_PAGE_SIZE
    queryClient.setQueryData<DeviceEventItem[]>(deviceKeys.events(props.deviceId), [...current, ...older])
  } finally {
    loadingOlder.value = false
  }
}
</script>

<template>
  <div class="flex flex-col gap-2">
    <div>
      <Button
        :label="expanded ? t('devices.events.hide') : t('devices.events.show')"
        :icon="expanded ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"
        size="small"
        text
        severity="secondary"
        data-testid="toggle-events"
        @click="expanded = !expanded"
      />
    </div>

    <div v-if="expanded" class="flex flex-col gap-2 ps-2" data-testid="device-events-panel">
      <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
      <Skeleton v-else-if="isPending" height="3rem" />
      <p v-else-if="!events?.length" class="text-sm text-muted-color">{{ t('devices.events.empty') }}</p>
      <template v-else>
        <ul class="flex flex-col gap-1.5 text-sm">
          <li
            v-for="item in events"
            :key="item.id"
            class="flex flex-wrap items-baseline justify-between gap-2 border-b border-surface-100 pb-1 last:border-b-0 dark:border-surface-800"
          >
            <div class="flex flex-wrap items-center gap-2">
              <span class="font-medium">{{ labelFor(item.name) }}</span>
              <Tag v-if="item.isLocation" :value="t('devices.events.location')" severity="secondary" />
              <span v-if="formatPayload(item.data)" class="font-mono text-xs text-muted-color">
                {{ formatPayload(item.data) }}
              </span>
            </div>
            <span class="text-xs text-muted-color">{{ d(new Date(item.occurredAt), 'long') }}</span>
          </li>
        </ul>
        <div v-if="hasMore">
          <Button
            :label="t('devices.events.older')"
            size="small"
            text
            :loading="loadingOlder"
            data-testid="events-older"
            @click="loadOlder"
          />
        </div>
      </template>
    </div>
  </div>
</template>
