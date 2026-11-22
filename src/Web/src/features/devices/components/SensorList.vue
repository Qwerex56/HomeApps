<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { DescriptorSensor } from '../api'

const props = defineProps<{
  sensors: DescriptorSensor[]
  values: Record<string, unknown>
  dimmed?: boolean
}>()
const { t } = useI18n()

function formatValue(sensor: DescriptorSensor): string {
  const val = props.values[sensor.name]
  if (val === undefined || val === null) return t('devices.sensors.noValue')
  const text = typeof val === 'object' ? JSON.stringify(val) : String(val)
  return sensor.unit ? `${text} ${sensor.unit}` : text
}
</script>

<template>
  <div v-if="sensors.length" class="flex flex-col gap-1.5" :class="{ 'opacity-60': dimmed }">
    <div class="text-xs font-semibold tracking-wide text-muted-color uppercase">
      {{ t('devices.sensors.title') }}
    </div>
    <dl class="grid grid-cols-2 gap-2 sm:grid-cols-3">
      <div
        v-for="sensor in sensors"
        :key="sensor.name"
        class="rounded-md bg-surface-100 px-3 py-2 dark:bg-surface-800"
      >
        <dt class="truncate text-xs text-muted-color">{{ sensor.label || sensor.name }}</dt>
        <dd class="font-mono text-sm font-medium">{{ formatValue(sensor) }}</dd>
      </div>
    </dl>
  </div>
</template>
