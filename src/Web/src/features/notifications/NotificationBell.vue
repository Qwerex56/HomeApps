<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import Button from 'primevue/button'
import Badge from 'primevue/badge'
import Drawer from 'primevue/drawer'
import { notificationKeys, notificationsApi, type InAppNotification } from './api'

// 2.13: the bell in the top bar with the unread count; a click opens the notification center.
const { t, d } = useI18n()
const router = useRouter()
const queryClient = useQueryClient()
const open = ref(false)

const { data, isError } = useQuery({
  queryKey: notificationKeys.inApp,
  queryFn: () => notificationsApi.inApp(),
  refetchInterval: 60_000,
})
const unreadCount = computed(() => data.value?.unreadCount ?? 0)
const items = computed(() => data.value?.items ?? [])

const refresh = () => queryClient.invalidateQueries({ queryKey: notificationKeys.inApp })
const readOne = useMutation({ mutationFn: notificationsApi.markRead, onSuccess: refresh })
const readAll = useMutation({ mutationFn: notificationsApi.markAllRead, onSuccess: refresh })

async function show(item: InAppNotification) {
  if (!item.readAt) readOne.mutate(item.id)
  open.value = false
  await router.push(item.url)
}
</script>

<template>
  <Button
    text
    rounded
    class="relative"
    :aria-label="t('notifications.center.open')"
    data-testid="notification-bell"
    @click="open = true"
  >
    <i class="pi pi-bell" aria-hidden="true" />
    <Badge v-if="unreadCount > 0" :value="unreadCount > 99 ? '99+' : unreadCount" severity="danger" data-testid="notification-badge" />
  </Button>
  <Drawer v-model:visible="open" position="right" :header="t('notifications.center.title')" class="!w-full sm:!w-96">
    <div class="flex flex-col gap-3">
      <Button
        v-if="unreadCount > 0"
        :label="t('notifications.center.markAll')"
        icon="pi pi-check"
        text
        size="small"
        class="self-end"
        :loading="readAll.isPending.value"
        data-testid="notification-read-all"
        @click="readAll.mutate()"
      />
      <p v-if="isError" class="text-muted-color">{{ t('notifications.center.failed') }}</p>
      <p v-else-if="!items.length" class="text-muted-color">{{ t('notifications.center.empty') }}</p>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li v-for="item in items" :key="item.id">
          <button
            type="button"
            class="flex w-full flex-col gap-1 py-3 text-start hover:bg-emphasis"
            :class="{ 'opacity-60': item.readAt }"
            data-testid="notification-item"
            @click="show(item)"
          >
            <span class="flex items-center gap-2 font-medium">
              <span v-if="!item.readAt" class="size-2 shrink-0 rounded-full bg-primary" aria-hidden="true" />
              <span class="truncate">{{ item.title }}</span>
            </span>
            <span class="line-clamp-2 text-sm text-muted-color">{{ item.body }}</span>
            <span class="text-xs text-muted-color">{{ d(new Date(item.createdAt), 'short') }}</span>
          </button>
        </li>
      </ul>
    </div>
  </Drawer>
</template>
