<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

const tabs = [
  { name: 'admin-users', label: 'admin.tabs.users', icon: 'pi pi-users' },
  { name: 'admin-households', label: 'admin.tabs.households', icon: 'pi pi-home' },
]
</script>

<template>
  <div class="flex flex-col gap-4">
    <nav :aria-label="t('app.admin')" class="flex gap-1 overflow-x-auto">
      <RouterLink
        v-for="tab in tabs"
        :key="tab.name"
        :to="{ name: tab.name }"
        class="flex items-center gap-2 rounded-md px-3 py-2 text-sm whitespace-nowrap text-muted-color hover:bg-emphasis"
        active-class="!text-primary bg-highlight"
      >
        <i :class="tab.icon" aria-hidden="true" />
        <span>{{ t(tab.label) }}</span>
      </RouterLink>
    </nav>
    <RouterView />
  </div>
</template>
