<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import ToggleSwitch from 'primevue/toggleswitch'
import Message from 'primevue/message'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { householdKeys, type HouseholdRole } from '@/features/households/api'
import ControlWidget from './ControlWidget.vue'
import SensorList from './SensorList.vue'
import DeviceEvents from './DeviceEvents.vue'
import DeviceAdminPanel from './DeviceAdminPanel.vue'
import { deviceKeys, deviceState, devicesApi, type Device, type PairingCode } from '../api'

// One device card: household device (assistant / smart_home) or personal computer.
const props = defineProps<{
  device: Device
  manage: boolean
  role?: HouseholdRole | null
}>()
const emit = defineEmits<{ code: [code: PairingCode] }>()
const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const busy = ref(false)

const isHousehold = computed(() => props.device.householdId !== null)
const canControl = computed(() =>
  isHousehold.value ? props.role === 'Member' || props.role === 'Admin' || props.role === 'Owner' : props.manage,
)
const canSeeEvents = computed(() =>
  isHousehold.value ? props.role === 'Member' || props.role === 'Admin' || props.role === 'Owner' : false,
)

const state = computed(() => deviceState(props.device))
const severity = {
  online: 'success',
  offline: 'secondary',
  silent: 'warn',
  unpaired: 'warn',
  removed: 'danger',
} as const

const iconClass = computed(() => {
  const purpose = props.device.purpose ?? (props.device.kind === 'Computer' ? 'computer' : 'assistant')
  if (purpose === 'computer') return 'pi pi-desktop'
  if (purpose === 'smart_home') return 'pi pi-sliders-h'
  return 'pi pi-microphone'
})

const controls = computed(() => props.device.descriptor?.controls ?? [])
const sensors = computed(() => props.device.descriptor?.sensors ?? [])
const declaredEvents = computed(() => props.device.descriptor?.events ?? [])
const values = computed(() => props.device.state?.values ?? {})
const sleepy = computed(() => Boolean(props.device.descriptor?.sleepy))

const controlDisabledReason = computed<string | null>(() => {
  if (props.device.removed) return t('devices.locked.removed')
  if (!props.device.paired) return t('devices.locked.unpaired')
  if (!canControl.value) return t('devices.locked.guest')
  if (sleepy.value) return t('devices.sleepyLocked')
  if (props.device.state?.silent) return t('devices.locked.silent')
  if (!props.device.online) return t('devices.locked.offline')
  return null
})

// Pending control changes waiting for either a `state` frame update or the 20s timeout (`devices.no_answer`).
const pendingControls = ref<Record<string, ReturnType<typeof setTimeout>>>({})

function clearPending(controlName: string) {
  const timer = pendingControls.value[controlName]
  if (timer) {
    clearTimeout(timer)
    const next = { ...pendingControls.value }
    delete next[controlName]
    pendingControls.value = next
  }
}

watch(
  () => props.device.state?.valuesAt,
  () => {
    for (const key of Object.keys(pendingControls.value)) {
      clearPending(key)
    }
  },
)

onBeforeUnmount(() => {
  for (const timer of Object.values(pendingControls.value)) {
    clearTimeout(timer)
  }
})

async function setControlValue(controlName: string, value: unknown) {
  clearPending(controlName)
  const timeout = setTimeout(() => {
    clearPending(controlName)
    toast.add({ severity: 'error', summary: t('errors.devices.no_answer'), life: 6000 })
  }, 20_000)
  pendingControls.value = { ...pendingControls.value, [controlName]: timeout }

  try {
    await devicesApi.setControl(props.device.id, controlName, value)
  } catch (error) {
    clearPending(controlName)
    const problem = toProblem(error)
    if (problem.status === 403 && props.device.householdId) {
      void queryClient.invalidateQueries({ queryKey: householdKeys.all })
    }
    if (problem.status === 404 || problem.code === 'devices.not_found') {
      toast.add({ severity: 'warn', summary: t('devices.removedToast'), life: 5000 })
      void queryClient.invalidateQueries({ queryKey: deviceKeys.all })
      return
    }
    toast.add({ severity: 'error', summary: errorMessage(problem), life: 6000 })
  }
}

async function run(action: () => Promise<unknown>) {
  busy.value = true
  try {
    await action()
    await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
  } catch (error) {
    const problem = toProblem(error)
    if (problem.status === 403 && props.device.householdId) {
      void queryClient.invalidateQueries({ queryKey: householdKeys.all })
    }
    if (problem.status === 404 || problem.code === 'devices.not_found') {
      toast.add({ severity: 'warn', summary: t('devices.removedToast'), life: 5000 })
      await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
      return
    }
    toast.add({ severity: 'error', summary: errorMessage(problem), life: 6000 })
  } finally {
    busy.value = false
  }
}

const restore = () => run(() => devicesApi.restore(props.device.id))
const allow = (action: string, allowed: boolean) => run(() => devicesApi.allow(props.device.id, action, allowed))

function remove() {
  confirm.require({
    header: t('devices.removeTitle', { name: props.device.name }),
    message: isHousehold.value ? t('devices.removeSatellite') : t('devices.removeComputer'),
    acceptProps: { label: t('devices.remove'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => run(() => devicesApi.remove(props.device.id)),
  })
}

function newCode() {
  const make = () => run(async () => emit('code', await devicesApi.newCode(props.device.id)))
  if (!props.device.paired) return make()
  confirm.require({
    header: t('devices.newCodeTitle'),
    message: t('devices.newCodeText', { name: props.device.name }),
    acceptProps: { label: t('devices.newCode') },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: make,
  })
}
</script>

<template>
  <li
    class="flex flex-col gap-3 rounded-xl border border-surface-200 p-4 dark:border-surface-700"
    :data-testid="`device-card-${device.id}`"
  >
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div class="flex min-w-0 items-center gap-3">
        <i :class="iconClass" class="text-lg text-muted-color" aria-hidden="true" />
        <div class="min-w-0">
          <div class="truncate font-medium">{{ device.name }}</div>
          <div class="flex flex-wrap items-center gap-2 text-xs text-muted-color">
            <span v-if="device.descriptor?.model" class="font-mono">
              {{ device.descriptor.model }}
              <template v-if="device.descriptor.version">v{{ device.descriptor.version }}</template>
            </span>
            <span v-if="device.state?.valuesAt">
              {{ t('devices.lastReading', { when: d(new Date(device.state.valuesAt), 'long') }) }}
            </span>
            <span v-else>
              {{ t('devices.lastSeen', { when: d(new Date(device.lastSeenAt), 'long') }) }}
            </span>
          </div>
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <Tag :value="t(`devices.state.${state}`)" :severity="severity[state]" />
        <Tag
          v-if="device.state?.lowBattery"
          :value="
            device.state.battery != null
              ? t('devices.state.lowBattery', { battery: Math.round(device.state.battery) })
              : t('devices.state.lowBatteryShort')
          "
          severity="danger"
        />
        <span
          v-else-if="device.state?.battery != null"
          class="text-xs text-muted-color"
          data-testid="device-battery"
        >
          {{ t('devices.health.battery', { battery: Math.round(device.state.battery) }) }}
        </span>
        <span v-if="device.state?.rssi != null" class="text-xs text-muted-color" data-testid="device-rssi">
          {{ t('devices.health.rssi', { rssi: device.state.rssi }) }}
        </span>

        <template v-if="manage">
          <Button
            v-if="isHousehold && !device.removed"
            :label="device.paired ? t('devices.newCode') : t('devices.showCode')"
            icon="pi pi-key"
            severity="secondary"
            outlined
            size="small"
            :loading="busy"
            @click="newCode"
          />
          <Button
            v-if="device.removed"
            :label="t('devices.restore')"
            severity="secondary"
            outlined
            size="small"
            :loading="busy"
            @click="restore"
          />
          <Button
            v-else
            :label="t('devices.remove')"
            severity="danger"
            text
            size="small"
            :loading="busy"
            @click="remove"
          />
        </template>
      </div>
    </div>

    <Message
      v-if="manage && device.descriptor?.warnings?.length"
      severity="warn"
      :closable="false"
      data-testid="descriptor-warnings"
    >
      <div class="text-xs">
        <div class="font-medium">{{ t('devices.warnings') }}</div>
        <ul class="list-inside list-disc">
          <li v-for="warning in device.descriptor.warnings" :key="warning">{{ warning }}</li>
        </ul>
      </div>
    </Message>

    <SensorList
      v-if="sensors.length && !device.removed"
      :sensors="sensors"
      :values="values"
      :dimmed="Boolean(device.state?.silent || !device.online)"
    />

    <div v-if="controls.length && !device.removed" class="flex flex-col gap-1">
      <div class="text-xs font-semibold tracking-wide text-muted-color uppercase">
        {{ t('devices.controls.title') }}
      </div>
      <ControlWidget
        v-for="control in controls"
        :key="control.name"
        :control="control"
        :value="values[control.name]"
        :disabled-reason="controlDisabledReason"
        :pending="Boolean(pendingControls[control.name])"
        @set="(val) => setControlValue(control.name, val)"
      />
    </div>

    <!-- Personal computers (or legacy devices without a descriptor): per-action "Nie pytaj". -->
    <ul v-if="!isHousehold && device.actions.length && !device.removed" class="flex flex-col gap-2 ps-4">
      <li v-for="action in device.actions" :key="action.name" class="flex items-center justify-between gap-4">
        <div class="min-w-0">
          <div class="text-sm">
            <code>{{ action.name }}</code>
            <Tag v-if="action.readOnly" :value="t('devices.readOnly')" severity="secondary" class="ms-2" />
          </div>
          <div class="text-sm text-muted-color">{{ action.description }}</div>
        </div>
        <label
          v-if="action.confirm && !action.readOnly"
          class="flex shrink-0 items-center gap-2 text-sm text-muted-color"
        >
          <span>{{ t('devices.dontAsk') }}</span>
          <ToggleSwitch
            :model-value="action.autoApprove"
            :disabled="!manage || busy"
            @update:model-value="(allowed: boolean) => allow(action.name, allowed)"
          />
        </label>
      </li>
    </ul>

    <DeviceEvents
      v-if="canSeeEvents && !device.removed && (declaredEvents.length || device.descriptor?.described)"
      :device-id="device.id"
      :declared-events="declaredEvents"
    />

    <DeviceAdminPanel v-if="isHousehold && !device.removed" :device="device" :can-edit="manage" />
  </li>
</template>

