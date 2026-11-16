<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { MyListing } from '../api'

// Installs a day for 30 days, as plain bars: one number a day needs no chart library.
const props = defineProps<{ days: MyListing['days'] }>()
const { t, d } = useI18n()
const bars = computed(() => {
  const counts = props.days.map((day) => Number(day.installs))
  const max = Math.max(1, ...counts)
  return props.days.map((day, i) => ({
    day: day.day,
    n: counts[i]!,
    height: `${Math.max(2, (counts[i]! / max) * 100)}%`,
  }))
})
</script>

<template>
  <figure class="flex flex-col gap-1">
    <figcaption class="text-sm text-muted-color">{{ t('homemix.store.account.chart') }}</figcaption>
    <div class="flex h-24 items-end gap-0.5" role="img" :aria-label="t('homemix.store.account.chart')">
      <div
        v-for="bar in bars"
        :key="bar.day"
        class="flex-1 rounded-t-sm"
        :class="bar.n ? 'bg-primary' : 'bg-surface-200 dark:bg-surface-700'"
        :style="{ height: bar.height }"
        :title="t('homemix.store.account.day', { date: d(bar.day, 'short'), n: bar.n })"
      />
    </div>
  </figure>
</template>
