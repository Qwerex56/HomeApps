<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

const tabs = [
  { name: 'account-profile', label: 'account.tabs.profile', icon: 'pi pi-user' },
  { name: 'account-security', label: 'account.tabs.security', icon: 'pi pi-lock' },
  { name: 'account-sessions', label: 'account.tabs.sessions', icon: 'pi pi-desktop' },
  { name: 'account-invitations', label: 'account.tabs.invitations', icon: 'pi pi-send' },
  { name: 'account-history', label: 'account.tabs.history', icon: 'pi pi-history' },
]
</script>

<template>
  <div class="flex flex-col gap-6 md:flex-row">
    <nav :aria-label="t('app.account')" class="flex gap-1 overflow-x-auto md:w-52 md:shrink-0 md:flex-col">
      <RouterLink
        v-for="tab in tabs"
        :key="tab.name"
        :to="{ name: tab.name }"
        class="flex items-center gap-2 rounded-md px-3 py-2 text-sm whitespace-nowrap text-muted-color hover:bg-emphasis"
        exact-active-class="!text-primary bg-highlight"
      >
        <i :class="tab.icon" aria-hidden="true" />
        <span>{{ t(tab.label) }}</span>
      </RouterLink>
    </nav>
    <section class="min-w-0 flex-1">
      <RouterView />
    </section>
  </div>
</template>
