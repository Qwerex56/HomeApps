<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { localized } from '@/features/plugins/registry'
import { appLink, useMyApps } from './myApps'
import PinDialog from './PinDialog.vue'

// The quick links bar under the header (UI 2.4.1): up to 8 add-ons of my account, ✎ to choose them. Hidden while I
// have no add-ons on my account; the rest are in "Aplikacje".
const { t } = useI18n()
const { apps, pinned } = useMyApps()
const editing = ref(false)
</script>

<template>
  <nav
    v-if="apps.length"
    :aria-label="t('quickLinks.label')"
    class="mx-auto flex h-10 max-w-6xl items-center gap-1 overflow-x-auto px-4 text-sm"
  >
    <RouterLink
      v-for="app in pinned"
      :key="app.id"
      :to="appLink(app)"
      class="flex shrink-0 items-center gap-2 rounded-full px-3 py-1 whitespace-nowrap text-muted-color hover:bg-emphasis"
      active-class="!text-primary bg-highlight"
    >
      <i :class="['pi', app.icon]" aria-hidden="true" />
      <span>{{ localized(app.name) }}</span>
    </RouterLink>
    <button
      type="button"
      class="flex shrink-0 items-center gap-2 rounded-full px-3 py-1 whitespace-nowrap text-muted-color hover:bg-emphasis"
      :aria-label="t('quickLinks.edit')"
      :title="t('quickLinks.edit')"
      @click="editing = true"
    >
      <i class="pi pi-pencil" aria-hidden="true" />
      <span v-if="!pinned.length">{{ t('quickLinks.pin') }}</span>
    </button>
    <PinDialog v-model:visible="editing" />
  </nav>
</template>
