<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import type { Household, HouseholdPermissions } from '@/features/households/api'
import AssistantCard from '../components/AssistantCard.vue'
import DeviceCard from '../components/DeviceCard.vue'
import PairingCodeDialog from '../components/PairingCodeDialog.vue'
import { useDeviceStream } from '../useDeviceStream'
import { assistantKeys, deviceKeys, devicesApi, type Device, type PairingCode } from '../api'

// The household's JARVIS tab (H-9, 2.17): left column AssistantCard, top "Dodaj urządzenie" + link to "Moje komputery",
// center device cards grouped by purpose (assistant / smart_home), live updates via SSE stream with 30s fallback.
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()
const id = computed(() => props.household.id)

const code = ref<PairingCode | null>(null)
const addInput = ref<{ $el?: HTMLElement } | null>(null)

const { connected: streamConnected, liveLost } = useDeviceStream(id, {
  onEvent(event) {
    if (
      event.type === 'changed' &&
      event.data.reason === 'paired' &&
      code.value &&
      event.data.deviceId === code.value.deviceId
    ) {
      const pairedName = code.value.name
      code.value = null
      toast.add({ severity: 'success', summary: t('devices.pairedToast', { name: pairedName }), life: 4000 })
    }
  },
})

const { data: assistant } = useQuery({
  queryKey: computed(() => assistantKeys.household(id.value)),
  queryFn: () => devicesApi.assistant(id.value),
})

const {
  data: devices,
  error,
  isPending,
} = useQuery({
  queryKey: computed(() => deviceKeys.household(id.value)),
  queryFn: () => devicesApi.household(id.value, true),
  // Fallback polling every 30 s only when the live stream is not connected.
  refetchInterval: computed(() => (liveLost.value && !streamConnected.value ? 30_000 : false)),
})

const assistantDevices = computed(() =>
  (devices.value ?? []).filter((d) => (d.purpose ?? 'assistant') === 'assistant'),
)
const smartHomeDevices = computed(() =>
  (devices.value ?? []).filter((d) => d.purpose === 'smart_home'),
)

// When the pairing dialog is open and the stream is NOT connected, poll every 3s (up to 10 min) until paired.
// Stream and polling are mutually exclusive.
let pairingPollTimer: ReturnType<typeof setInterval> | null = null
let pairingStartedAt = 0

function stopPairingPoll() {
  if (pairingPollTimer) {
    clearInterval(pairingPollTimer)
    pairingPollTimer = null
  }
}

watch(
  () => [code.value, streamConnected.value] as const,
  ([currentCode, isLive]) => {
    stopPairingPoll()
    if (!currentCode) return
    if (!pairingStartedAt) pairingStartedAt = Date.now()
    if (isLive) return
    pairingPollTimer = setInterval(() => {
      if (!code.value || Date.now() - pairingStartedAt > 10 * 60_000) {
        stopPairingPoll()
        return
      }
      void queryClient.invalidateQueries({ queryKey: deviceKeys.household(id.value) })
    }, 3000)
  },
)

watch(code, (val) => {
  if (!val) pairingStartedAt = 0
})

watch(devices, (list: Device[] | undefined) => {
  if (!code.value || !list) return
  const match = list.find((d) => d.id === code.value!.deviceId)
  if (match?.paired) {
    const pairedName = match.name
    code.value = null
    toast.add({ severity: 'success', summary: t('devices.pairedToast', { name: pairedName }), life: 4000 })
  }
})

onBeforeUnmount(() => stopPairingPoll())

const name = ref('')
const form = useSubmit()
async function add() {
  const cleanName = name.value.trim()
  const ok = await form.submit(
    async () => {
      code.value = await devicesApi.addSatellite(id.value, cleanName)
    },
    (problem) => {
      if (problem.code === 'devices.name_taken' || problem.code === 'devices.invalid_name') {
        form.fieldErrors.value = { name: [errorMessage(problem)] }
        return true
      }
    },
  )
  if (!ok) return
  name.value = ''
  await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
}

function focusAdd() {
  const input = document.getElementById('satelliteName')
  input?.focus()
  addInput.value?.$el?.scrollIntoView?.({ behavior: 'smooth' })
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <Message
      v-if="assistant && !assistant.enabled"
      severity="warn"
      :closable="false"
      data-testid="assistant-disabled-banner"
    >
      {{ t('jarvis.assistant.disabledBanner') }}
    </Message>

    <Message v-if="liveLost" severity="warn" :closable="false" data-testid="live-lost-banner">
      {{ t('devices.liveLost') }}
    </Message>

    <div class="grid grid-cols-1 items-start gap-6 lg:grid-cols-12">
      <!-- Left column: JARVIS domu (stacks above the list on mobile). -->
      <div class="lg:col-span-4">
        <AssistantCard :household-id="id" :can-edit="can.edit" />
      </div>

      <!-- Main column: Add device, link to My Computers, and grouped device cards. -->
      <div class="flex flex-col gap-6 lg:col-span-8">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-lg font-semibold">{{ t('devices.household') }}</h2>
            <p class="text-sm text-muted-color">{{ t('devices.householdHint') }}</p>
          </div>
          <RouterLink
            :to="{ name: 'account-devices' }"
            class="flex items-center gap-2 text-sm text-primary hover:underline"
            data-testid="my-computers-link"
          >
            <i class="pi pi-desktop" aria-hidden="true" />
            <span>{{ t('devices.myComputersLink') }}</span>
          </RouterLink>
        </div>

        <Card v-if="can.edit" ref="addInput">
          <template #title>{{ t('devices.add') }}</template>
          <template #content>
            <form class="flex flex-col gap-4" novalidate data-testid="add-device-form" @submit.prevent="add">
              <Message v-if="form.formError.value" severity="error" :closable="false">
                {{ form.formError.value }}
              </Message>
              <FormField id="satelliteName" :label="t('devices.name')" :errors="form.fieldErrors.value.name">
                <template #default="{ invalid }">
                  <InputText
                    id="satelliteName"
                    v-model="name"
                    :placeholder="t('devices.namePlaceholder')"
                    maxlength="60"
                    :invalid="invalid"
                  />
                </template>
              </FormField>
              <div class="flex justify-end">
                <Button
                  type="submit"
                  :label="t('devices.addButton')"
                  icon="pi pi-plus"
                  :disabled="!name.trim()"
                  :loading="form.busy.value"
                />
              </div>
            </form>
          </template>
        </Card>

        <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
        <Skeleton v-else-if="isPending" height="8rem" />
        <Card v-else-if="!devices?.length">
          <template #content>
            <div class="flex flex-col items-start gap-3">
              <p class="text-muted-color">{{ t('devices.noSatellites') }}</p>
              <Button
                v-if="can.edit"
                :label="t('devices.add')"
                icon="pi pi-plus"
                size="small"
                outlined
                data-testid="empty-add-cta"
                @click="focusAdd"
              />
            </div>
          </template>
        </Card>
        <template v-else>
          <section v-if="assistantDevices.length" class="flex flex-col gap-3" data-testid="group-assistant">
            <h3 class="text-sm font-semibold tracking-wide text-muted-color uppercase">
              {{ t('devices.groups.assistant') }}
            </h3>
            <ul class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
              <DeviceCard
                v-for="device in assistantDevices"
                :key="device.id"
                :device="device"
                :manage="can.edit"
                :role="household.myRole"
                @code="code = $event"
              />
            </ul>
          </section>

          <section v-if="smartHomeDevices.length" class="flex flex-col gap-3" data-testid="group-smart-home">
            <h3 class="text-sm font-semibold tracking-wide text-muted-color uppercase">
              {{ t('devices.groups.smartHome') }}
            </h3>
            <ul class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
              <DeviceCard
                v-for="device in smartHomeDevices"
                :key="device.id"
                :device="device"
                :manage="can.edit"
                :role="household.myRole"
                @code="code = $event"
              />
            </ul>
          </section>
        </template>
      </div>
    </div>

    <PairingCodeDialog :code="code" @close="code = null" />
  </div>
</template>

