<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import { localized } from '@/features/plugins/registry'
import { appLink, useMyApps } from './myApps'
import HomeMixSettings from './HomeMixSettings.vue'
import MixWidgets from './MixWidgets.vue'
import PinDialog from './PinDialog.vue'

// "Aplikacje" (UI 2.4.1): every add-on on my account to open in one click, their widgets, and adding or managing
// them (what "Moje aplikacje" in the account was). The quick links bar holds up to 8 of them.
const { t } = useI18n()
const { apps, isPending } = useMyApps()
const editing = ref(false)
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-2xl font-semibold">{{ t('app.apps') }}</h1>
      <div class="flex gap-2">
        <Button v-if="apps.length" :label="t('quickLinks.edit')" icon="pi pi-pencil" outlined @click="editing = true" />
        <RouterLink :to="{ name: 'store' }">
          <Button :label="t('app.store')" icon="pi pi-shop" text />
        </RouterLink>
      </div>
    </div>

    <Skeleton v-if="isPending" height="6rem" />
    <ul v-else-if="apps.length" class="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-6">
      <li v-for="app in apps" :key="app.id">
        <RouterLink
          :to="appLink(app)"
          class="flex flex-col items-center gap-2 rounded-xl border border-surface bg-surface-0 p-4 text-center hover:border-primary dark:bg-surface-900"
        >
          <i :class="['pi', app.icon]" class="text-2xl text-primary" aria-hidden="true" />
          <span class="w-full truncate text-sm">{{ localized(app.name) }}</span>
          <i
            v-if="app.pin != null"
            class="pi pi-bookmark-fill text-xs text-muted-color"
            :title="t('quickLinks.onBar')"
          />
        </RouterLink>
      </li>
    </ul>

    <MixWidgets :household="null" place="Account" />
    <HomeMixSettings :target="{ kind: 'User' }" can-manage />
    <PinDialog v-model:visible="editing" />
  </div>
</template>
