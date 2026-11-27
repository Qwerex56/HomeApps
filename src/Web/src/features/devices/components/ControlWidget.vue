<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import ToggleSwitch from 'primevue/toggleswitch'
import Slider from 'primevue/slider'
import Select from 'primevue/select'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import type { DescriptorControl } from '../api'

// Renders one control by its widget ('switch' | 'slider' | 'select' | 'button') or JSON Schema type.
// Never writes optimistic values to the parent state; slider commits on slideend (@slideend / @change).
const props = defineProps<{
  control: DescriptorControl
  value: unknown
  disabledReason: string | null
  pending: boolean
  /** A sleepy device has not applied the value yet; the server keeps it until the device reports. */
  awaitingDevice?: boolean
}>()
const emit = defineEmits<{ set: [value: unknown] }>()
const { t } = useI18n()

const schema = computed(() => props.control.value ?? {})
const kind = computed<'switch' | 'slider' | 'select' | 'button' | 'number' | 'text'>(() => {
  const w = props.control.widget
  if (w === 'switch' || w === 'slider' || w === 'select' || w === 'button') return w
  if (schema.value.enum?.length) return 'select'
  if (schema.value.const !== undefined) return 'button'
  if (schema.value.type === 'boolean') return 'switch'
  if (
    (schema.value.type === 'integer' || schema.value.type === 'number') &&
    typeof schema.value.minimum === 'number' &&
    typeof schema.value.maximum === 'number'
  ) {
    return 'slider'
  }
  if (schema.value.type === 'integer' || schema.value.type === 'number') return 'number'
  return 'text'
})

const isDisabled = computed(() => !!props.disabledReason || props.pending || props.awaitingDevice)
const labelText = computed(() => props.control.label || props.control.name)

// Slider local drag position so sliding is smooth while only committing on release (no optimistic cache write).
const sliderDraft = ref<number>(0)
watch(
  () => props.value,
  (val) => {
    const num = typeof val === 'number' ? val : (schema.value.minimum ?? 0)
    sliderDraft.value = num
  },
  { immediate: true },
)

const numberDraft = ref<number | null>(null)
const textDraft = ref<string>('')
watch(
  () => props.value,
  (val) => {
    if (typeof val === 'number') numberDraft.value = val
    if (typeof val === 'string') textDraft.value = val
  },
  { immediate: true },
)

const selectOptions = computed(() => {
  if (schema.value.enum?.length) {
    return schema.value.enum.map((v) => ({ label: String(v), value: v }))
  }
  if (
    schema.value.type === 'integer' &&
    typeof schema.value.minimum === 'number' &&
    typeof schema.value.maximum === 'number' &&
    schema.value.maximum - schema.value.minimum <= 20
  ) {
    const opts: { label: string; value: number }[] = []
    for (let i = schema.value.minimum; i <= schema.value.maximum; i++) {
      opts.push({ label: String(i), value: i })
    }
    return opts
  }
  return []
})

function commitSlider(val: number | number[]) {
  if (isDisabled.value) return
  const next = Array.isArray(val) ? (val[0] ?? 0) : val
  emit('set', next)
}

function commitButton() {
  if (isDisabled.value) return
  const target = schema.value.const !== undefined ? schema.value.const : true
  emit('set', target)
}
</script>

<template>
  <div
    class="flex flex-col gap-1.5 py-1.5"
    :data-testid="`control-${control.name}`"
    :aria-busy="pending || awaitingDevice ? 'true' : undefined"
  >
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div class="min-w-0">
        <div class="text-sm font-medium">{{ labelText }}</div>
        <div v-if="control.description" class="text-xs text-muted-color">{{ control.description }}</div>
      </div>

      <div class="flex items-center gap-2">
        <span v-if="awaitingDevice" class="text-xs text-muted-color" data-testid="control-awaiting-device">
          {{ t('devices.controls.awaitingDevice') }}
        </span>
        <span v-else-if="pending" class="text-xs text-muted-color" data-testid="control-pending">
          {{ t('devices.controls.pending') }}
        </span>

        <ToggleSwitch
          v-if="kind === 'switch'"
          :model-value="Boolean(value)"
          :disabled="isDisabled"
          :aria-label="labelText"
          @update:model-value="(next: boolean) => !isDisabled && emit('set', next)"
        />

        <div v-else-if="kind === 'slider'" class="flex w-44 items-center gap-3">
          <Slider
            v-model="sliderDraft"
            :min="schema.minimum ?? 0"
            :max="schema.maximum ?? 100"
            :step="schema.type === 'integer' ? 1 : 0.5"
            :disabled="isDisabled"
            :aria-label="labelText"
            class="flex-1"
            @slideend="(e) => commitSlider(e.value)"
          />
          <span class="w-8 text-end font-mono text-xs">{{ typeof value === 'number' ? value : sliderDraft }}</span>
        </div>

        <Select
          v-else-if="kind === 'select'"
          :model-value="value"
          :options="selectOptions"
          option-label="label"
          option-value="value"
          :disabled="isDisabled"
          :aria-label="labelText"
          class="min-w-28"
          @update:model-value="(next: unknown) => !isDisabled && emit('set', next)"
        />

        <Button
          v-else-if="kind === 'button'"
          :label="labelText"
          size="small"
          outlined
          :disabled="isDisabled"
          :loading="pending"
          @click="commitButton"
        />

        <div v-else-if="kind === 'number'" class="flex items-center gap-1">
          <InputNumber
            v-model="numberDraft"
            :min="schema.minimum"
            :max="schema.maximum"
            :disabled="isDisabled"
            :aria-label="labelText"
            size="small"
          />
          <Button
            icon="pi pi-check"
            size="small"
            outlined
            :disabled="isDisabled || numberDraft === null"
            @click="emit('set', numberDraft)"
          />
        </div>

        <div v-else class="flex items-center gap-1">
          <InputText
            v-model="textDraft"
            :maxlength="schema.maxLength"
            :disabled="isDisabled"
            :aria-label="labelText"
            size="small"
          />
          <Button
            icon="pi pi-check"
            size="small"
            outlined
            :disabled="isDisabled"
            @click="emit('set', textDraft)"
          />
        </div>
      </div>
    </div>
    <small v-if="disabledReason" class="text-xs text-muted-color" data-testid="control-disabled-reason">
      {{ disabledReason }}
    </small>
  </div>
</template>
