<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import ToggleSwitch from 'primevue/toggleswitch'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { deviceKeys, deviceState, devicesApi, type Device, type PairingCode } from '../api'

// One device: its state, what it can do for JARVIS and, for whoever manages it, "Nie pytaj", removing and a new code.
const props = defineProps<{ device: Device; manage: boolean }>()
const emit = defineEmits<{ code: [code: PairingCode] }>()
const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const busy = ref(false)

const state = computed(() => deviceState(props.device))
const severity = { online: 'success', offline: 'secondary', unpaired: 'warn', removed: 'danger' } as const
const satellite = computed(() => props.device.kind === 'Satellite')

async function run(action: () => Promise<unknown>) {
  busy.value = true
  try {
    await action()
    await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = false
  }
}

const restore = () => run(() => devicesApi.restore(props.device.id))
const allow = (action: string, allowed: boolean) => run(() => devicesApi.allow(props.device.id, action, allowed))

function remove() {
  confirm.require({
    header: t('devices.removeTitle', { name: props.device.name }),
    message: satellite.value ? t('devices.removeSatellite') : t('devices.removeComputer'),
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
  <li class="flex flex-col gap-3 py-4">
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div class="flex min-w-0 items-center gap-3">
        <i :class="satellite ? 'pi pi-microphone' : 'pi pi-desktop'" class="text-muted-color" aria-hidden="true" />
        <div class="min-w-0">
          <div class="truncate font-medium">{{ device.name }}</div>
          <div class="text-sm text-muted-color">
            {{ t('devices.lastSeen', { when: d(new Date(device.lastSeenAt), 'long') }) }}
          </div>
        </div>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <Tag :value="t(`devices.state.${state}`)" :severity="severity[state]" />
        <template v-if="manage">
          <Button
            v-if="satellite && !device.removed"
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
            v-else-if="!device.removed"
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

    <ul v-if="device.actions.length && !device.removed" class="flex flex-col gap-2 ps-8">
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
  </li>
</template>
