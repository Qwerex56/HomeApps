<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import Tag from 'primevue/tag'
import { localized } from '@/features/plugins/registry'
import type { StoreEntry } from '../api'
import HeartButton from './HeartButton.vue'

// One add-on on the store page; the whole card leads to its page.
defineProps<{ entry: StoreEntry }>()
const { t } = useI18n()
</script>

<template>
  <RouterLink
    :to="{ name: 'store-entry', params: { listingKey: entry.listingKey } }"
    class="flex h-full flex-col gap-3 rounded-lg border border-surface-200 p-4 hover:bg-emphasis dark:border-surface-700"
  >
    <div class="flex items-start gap-3">
      <i :class="['pi', entry.icon, 'mt-1 text-2xl text-primary']" aria-hidden="true" />
      <div class="min-w-0 flex-1">
        <div class="flex flex-wrap items-center gap-2 font-medium">
          <span class="truncate">{{ localized(entry.name) }}</span>
          <Tag v-if="entry.official" :value="t('homemix.store.official')" severity="success" />
          <Tag v-if="entry.mine" :value="t('homemix.store.mine')" severity="info" />
        </div>
        <div class="text-xs text-muted-color">{{ entry.version }}</div>
      </div>
    </div>
    <p v-if="entry.description" class="line-clamp-3 flex-1 text-sm text-muted-color">
      {{ localized(entry.description) }}
    </p>
    <div v-else class="flex-1" />
    <div v-if="entry.problem" class="text-sm text-orange-500">{{ t(`homemix.store.problem.${entry.problem}`) }}</div>
    <div class="flex items-center justify-between gap-2 text-sm text-muted-color">
      <span class="flex items-center gap-1">
        <i class="pi pi-download" aria-hidden="true" />{{ t('homemix.store.installs', Number(entry.installs)) }}
      </span>
      <HeartButton :entry="entry" />
    </div>
  </RouterLink>
</template>
