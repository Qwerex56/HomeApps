<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import ToggleSwitch from 'primevue/toggleswitch'
import Button from 'primevue/button'
import Message from 'primevue/message'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem, type Problem } from '@/api/problem'
import { householdKeys } from '@/features/households/api'
import { deviceKeys, devicesApi, type Device } from '../api'

// Household device JARVIS switches (visible to Guest/Member read-only, editable by Admin+)
// plus inline name and adminNotes editing and maker's jarvis hint (Admin+ only).
const props = defineProps<{
  device: Device
  canEdit: boolean
}>()
const { t } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()

const editing = ref(false)
const nameDraft = ref(props.device.name)
const notesDraft = ref(props.device.adminNotes ?? '')
const switchBusy = ref(false)

watch(
  () => [props.device.name, props.device.adminNotes] as const,
  ([n, notes]) => {
    if (!editing.value) {
      nameDraft.value = n
      notesDraft.value = notes ?? ''
    }
  },
)

function handleNotFoundOrForbidden(problem: Problem): boolean {
  if (problem.status === 404 || problem.code === 'devices.not_found') {
    editing.value = false
    toast.add({ severity: 'warn', summary: t('devices.removedToast'), life: 5000 })
    void queryClient.invalidateQueries({ queryKey: deviceKeys.all })
    return true
  }
  if (problem.status === 403) {
    void queryClient.invalidateQueries({ queryKey: householdKeys.all })
  }
  return false
}

async function toggleJarvis(mayControl: boolean, asks: boolean) {
  if (!props.canEdit) return
  switchBusy.value = true
  try {
    await devicesApi.setJarvis(props.device.id, mayControl, asks)
    await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
  } catch (error) {
    const problem = toProblem(error)
    if (!handleNotFoundOrForbidden(problem)) {
      toast.add({ severity: 'error', summary: errorMessage(problem), life: 6000 })
    }
  } finally {
    switchBusy.value = false
  }
}

const form = useSubmit()
async function saveAdminDetails() {
  if (!props.canEdit) return
  const cleanName = nameDraft.value.trim()
  const cleanNotes = notesDraft.value.trim() || null
  const ok = await form.submit(
    async () => {
      if (cleanName !== props.device.name) {
        await devicesApi.rename(props.device.id, cleanName)
      }
      if (cleanNotes !== (props.device.adminNotes ?? null)) {
        await devicesApi.setNotes(props.device.id, cleanNotes)
      }
    },
    (problem) => {
      if (handleNotFoundOrForbidden(problem)) return true
      if (problem.code === 'devices.name_taken' || problem.code === 'devices.invalid_name') {
        form.fieldErrors.value = { name: [errorMessage(problem)] }
        return true
      }
      if (problem.code === 'devices.invalid_notes') {
        form.fieldErrors.value = { notes: [errorMessage(problem)] }
        return true
      }
    },
  )
  if (!ok) return
  editing.value = false
  toast.add({ severity: 'success', summary: t('devices.jarvis.saved'), life: 3000 })
  await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
}
</script>

<template>
  <div class="flex flex-col gap-3 border-t border-surface-200 pt-3 dark:border-surface-700">
    <div class="flex flex-wrap items-center gap-6">
      <label class="flex items-center gap-2 text-sm">
        <ToggleSwitch
          :model-value="device.jarvisMayControl ?? true"
          :disabled="!canEdit || switchBusy"
          :aria-label="t('devices.jarvis.mayControl')"
          @update:model-value="(v: boolean) => toggleJarvis(v, device.jarvisAsks ?? true)"
        />
        <span>{{ t('devices.jarvis.mayControl') }}</span>
      </label>

      <label class="flex items-center gap-2 text-sm">
        <ToggleSwitch
          :model-value="device.jarvisAsks ?? true"
          :disabled="!canEdit || switchBusy || !(device.jarvisMayControl ?? true)"
          :aria-label="t('devices.jarvis.asks')"
          @update:model-value="(v: boolean) => toggleJarvis(device.jarvisMayControl ?? true, v)"
        />
        <span>{{ t('devices.jarvis.asks') }}</span>
      </label>
    </div>

    <template v-if="canEdit">
      <p v-if="device.descriptor?.jarvis" class="text-xs text-muted-color" data-testid="maker-jarvis-text">
        <span class="font-medium">{{ t('devices.jarvis.makerText') }}:</span> {{ device.descriptor.jarvis }}
      </p>

      <div v-if="!editing" class="flex flex-wrap items-center justify-between gap-2">
        <p v-if="device.adminNotes" class="text-sm text-muted-color" data-testid="admin-notes-display">
          <span class="font-medium">{{ t('devices.jarvis.notes') }}:</span> {{ device.adminNotes }}
        </p>
        <span v-else class="text-xs text-muted-color">{{ t('devices.jarvis.notes') }}: —</span>
        <Button
          :label="t('devices.jarvis.title')"
          icon="pi pi-pencil"
          size="small"
          text
          severity="secondary"
          data-testid="open-device-admin"
          @click="editing = true"
        />
      </div>

      <form v-else class="flex flex-col gap-3" novalidate data-testid="device-admin-form" @submit.prevent="saveAdminDetails">
        <Message v-if="form.formError.value" severity="error" :closable="false">{{ form.formError.value }}</Message>
        <FormField
          :id="`device-name-${device.id}`"
          :label="t('devices.name')"
          :errors="form.fieldErrors.value.name"
        >
          <template #default="{ invalid }">
            <InputText
              :id="`device-name-${device.id}`"
              v-model="nameDraft"
              maxlength="60"
              :invalid="invalid"
            />
          </template>
        </FormField>

        <FormField
          :id="`device-notes-${device.id}`"
          :label="t('devices.jarvis.notes')"
          :errors="form.fieldErrors.value.notes"
        >
          <template #default="{ invalid }">
            <Textarea
              :id="`device-notes-${device.id}`"
              v-model="notesDraft"
              :placeholder="t('devices.jarvis.notesPlaceholder')"
              maxlength="500"
              rows="2"
              :invalid="invalid"
            />
          </template>
        </FormField>

        <div class="flex justify-end gap-2">
          <Button
            :label="t('common.cancel')"
            size="small"
            severity="secondary"
            text
            @click="editing = false"
          />
          <Button
            type="submit"
            :label="t('common.save')"
            size="small"
            icon="pi pi-check"
            :loading="form.busy.value"
            :disabled="!nameDraft.trim()"
          />
        </div>
      </form>
    </template>
  </div>
</template>
