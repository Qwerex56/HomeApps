<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import ColorSchemeToggle from '@/shared/components/ColorSchemeToggle.vue'
import { useMyAccount } from '@/features/account/api'
import UserMenu from './UserMenu.vue'

const { t } = useI18n()
const { pending } = useMyAccount()

// The menu grows with each stage (account F4, admin F5, households F6).
const links = [{ to: { name: 'home' }, label: 'app.home', icon: 'pi pi-home' }]
</script>

<template>
  <div class="flex min-h-dvh flex-col">
    <header
      class="sticky top-0 z-10 border-b border-surface-200 bg-surface-0/90 backdrop-blur dark:border-surface-800 dark:bg-surface-900/90"
    >
      <div class="mx-auto flex h-14 max-w-6xl items-center gap-4 px-4">
        <RouterLink :to="{ name: 'home' }" class="flex items-center gap-2 font-semibold text-primary">
          <i class="pi pi-home text-xl" aria-hidden="true" />
          <span>{{ t('app.name') }}</span>
        </RouterLink>
        <nav class="flex flex-1 items-center gap-1 overflow-x-auto">
          <RouterLink
            v-for="link in links"
            :key="link.label"
            :to="link.to"
            class="flex items-center gap-2 rounded-md px-3 py-2 text-sm text-muted-color hover:bg-emphasis"
            active-class="!text-primary bg-highlight"
          >
            <i :class="link.icon" aria-hidden="true" />
            <span class="hidden sm:inline">{{ t(link.label) }}</span>
          </RouterLink>
        </nav>
        <!-- The household switcher (Q6) comes here in F6. -->
        <ColorSchemeToggle />
        <UserMenu />
      </div>
    </header>
    <main class="mx-auto w-full max-w-6xl flex-1 px-4 py-6 pb-[calc(1.5rem+env(safe-area-inset-bottom))]">
      <!-- Right after sign-up Accounts needs a moment to create the account (user.pending). -->
      <Message v-if="pending" severity="info" :closable="false" class="mb-4">{{ t('errors.user.pending') }}</Message>
      <RouterView />
    </main>
  </div>
</template>
