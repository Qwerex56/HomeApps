<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import SelectButton from 'primevue/selectbutton'
import Checkbox from 'primevue/checkbox'
import ToggleSwitch from 'primevue/toggleswitch'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { householdKeys } from '@/features/households/api'
import {
  ALL_TOOL_SOURCES,
  parseToolSources,
  serializeToolSources,
  type AssistantToolSource,
} from '../assistantFlags'
import { assistantKeys, devicesApi, type AssistantAccessLevel, type HouseholdAssistant } from '../api'

// Left column of the JARVIS tab: household assistant settings (Guest/Member read-only, Admin+ edit).
const props = defineProps<{ householdId: string; canEdit: boolean }>()
const { t } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()

const {
  data: assistant,
  error,
  isPending,
} = useQuery({
  queryKey: computed(() => assistantKeys.household(props.householdId)),
  queryFn: () => devicesApi.assistant(props.householdId),
})

const draft = reactive<{
  enabled: boolean
  name: string
  accessLevel: AssistantAccessLevel
  sources: AssistantToolSource[]
  eventDays: number
  locationEventDays: number
}>({
  enabled: true,
  name: 'JARVIS',
  accessLevel: 'ReadOnly',
  sources: [...ALL_TOOL_SOURCES],
  eventDays: 30,
  locationEventDays: 7,
})

function syncFrom(value: HouseholdAssistant | undefined) {
  if (!value) return
  draft.enabled = value.enabled
  draft.name = value.name
  draft.accessLevel = value.accessLevel
  draft.sources = parseToolSources(value.toolSources)
  draft.eventDays = value.eventDays
  draft.locationEventDays = value.locationEventDays
}

watch(assistant, (value) => syncFrom(value), { immediate: true })

const levelOptions = computed(() => [
  { value: 'ReadOnly' as const, label: t('jarvis.assistant.level.ReadOnly') },
  { value: 'Member' as const, label: t('jarvis.assistant.level.Member') },
])

const sourceKeys: Record<AssistantToolSource, string> = {
  Plugins: 'plugins',
  HomeMix: 'homemix',
  Devices: 'devices',
}

const localError = ref<string | null>(null)
const form = useSubmit()

function validate(): boolean {
  localError.value = null
  const cleanName = draft.name.trim()
  if (cleanName.length < 1 || cleanName.length > 32) {
    localError.value = t('errors.household.assistant_name_invalid')
    return false
  }
  const eventDays = Number(draft.eventDays)
  const locationDays = Number(draft.locationEventDays)
  if (
    !Number.isInteger(eventDays) ||
    eventDays < 1 ||
    eventDays > 365 ||
    !Number.isInteger(locationDays) ||
    locationDays < 1 ||
    locationDays > Math.min(90, eventDays)
  ) {
    localError.value = t('errors.household.assistant_retention_invalid')
    return false
  }
  return true
}

async function performSave() {
  if (!validate()) return
  const ok = await form.submit(
    async () => {
      await devicesApi.updateAssistant(props.householdId, {
        enabled: draft.enabled,
        name: draft.name.trim(),
        accessLevel: draft.accessLevel,
        toolSources: serializeToolSources(draft.sources),
        eventDays: Number(draft.eventDays),
        locationEventDays: Number(draft.locationEventDays),
      })
    },
    (problem) => {
      if (problem.status === 403) {
        void queryClient.invalidateQueries({ queryKey: householdKeys.all })
      }
      syncFrom(assistant.value)
    },
  )
  if (!ok) return
  toast.add({ severity: 'success', summary: t('jarvis.assistant.saved'), life: 3000 })
  await queryClient.invalidateQueries({ queryKey: assistantKeys.all })
}

function submitForm() {
  if (!props.canEdit) return
  if (assistant.value?.enabled && !draft.enabled) {
    confirm.require({
      header: t('jarvis.assistant.confirmOffTitle'),
      message: t('jarvis.assistant.confirmOff'),
      acceptProps: { label: t('common.save'), severity: 'danger' },
      rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
      accept: () => void performSave(),
      reject: () => syncFrom(assistant.value),
    })
    return
  }
  void performSave()
}
</script>

<template>
  <Card>
    <template #title>{{ t('jarvis.assistant.title') }}</template>
    <template #subtitle>{{ t('jarvis.assistant.subtitle') }}</template>
    <template #content>
      <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
      <Skeleton v-else-if="isPending" height="12rem" />
      <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="submitForm">
        <Message v-if="localError || form.formError.value" severity="error" :closable="false">
          {{ localError ?? form.formError.value }}
        </Message>

        <label class="flex items-center justify-between gap-3">
          <span class="text-sm font-medium">{{ t('jarvis.assistant.enabled') }}</span>
          <ToggleSwitch
            v-model="draft.enabled"
            input-id="assistant-enabled"
            :disabled="!canEdit || form.busy.value"
          />
        </label>

        <FormField
          id="assistantName"
          :label="t('jarvis.assistant.name')"
          :errors="form.fieldErrors.value.name"
        >
          <template #default="{ invalid }">
            <InputText
              id="assistantName"
              v-model="draft.name"
              maxlength="32"
              :disabled="!canEdit || form.busy.value"
              :invalid="invalid"
            />
          </template>
        </FormField>

        <FormField
          id="assistantLevel"
          :label="t('jarvis.assistant.levelLabel')"
          :errors="form.fieldErrors.value.accessLevel"
        >
          <SelectButton
            id="assistantLevel"
            v-model="draft.accessLevel"
            :options="levelOptions"
            option-label="label"
            option-value="value"
            :allow-empty="false"
            :disabled="!canEdit || form.busy.value"
          />
        </FormField>

        <fieldset class="flex flex-col gap-2">
          <legend class="mb-1 text-sm font-medium">{{ t('jarvis.assistant.sourcesLabel') }}</legend>
          <label
            v-for="source in ALL_TOOL_SOURCES"
            :key="source"
            class="flex items-center gap-2 text-sm"
          >
            <Checkbox
              v-model="draft.sources"
              :input-id="`assistant-source-${source}`"
              :value="source"
              :disabled="!canEdit || form.busy.value"
            />
            <span>{{ t(`jarvis.assistant.sources.${sourceKeys[source]}`) }}</span>
          </label>
        </fieldset>

        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormField
            id="assistantEventDays"
            :label="t('jarvis.assistant.eventDays')"
            :errors="form.fieldErrors.value.eventDays"
          >
            <template #default="{ invalid }">
              <InputNumber
                input-id="assistantEventDays"
                v-model="draft.eventDays"
                :min="1"
                :max="365"
                :disabled="!canEdit || form.busy.value"
                :invalid="invalid"
                fluid
              />
            </template>
          </FormField>

          <FormField
            id="assistantLocationEventDays"
            :label="t('jarvis.assistant.locationEventDays')"
            :errors="form.fieldErrors.value.locationEventDays"
          >
            <template #default="{ invalid }">
              <InputNumber
                input-id="assistantLocationEventDays"
                v-model="draft.locationEventDays"
                :min="1"
                :max="Math.min(90, draft.eventDays || 90)"
                :disabled="!canEdit || form.busy.value"
                :invalid="invalid"
                fluid
              />
            </template>
          </FormField>
        </div>

        <p v-if="!canEdit" class="text-xs text-muted-color">{{ t('devices.readOnlyHint') }}</p>
        <div v-else class="flex justify-end">
          <Button
            type="submit"
            :label="t('common.save')"
            icon="pi pi-check"
            :loading="form.busy.value"
            :disabled="!draft.name.trim()"
          />
        </div>
      </form>
    </template>
  </Card>
</template>
