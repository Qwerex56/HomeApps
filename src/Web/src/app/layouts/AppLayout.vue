<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, RouterView } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import ColorSchemeToggle from '@/shared/components/ColorSchemeToggle.vue'
import { useMyAccount } from '@/features/account/api'
import { useSessionStore } from '@/features/auth/session'
import HouseholdSwitcher from '@/features/households/components/HouseholdSwitcher.vue'
import UserMenu from './UserMenu.vue'

const { t } = useI18n()
const { pending } = useMyAccount()
const session = useSessionStore()

// Administration only for Admin and Owner.
const links = computed(() => [
  { to: { name: 'home' }, label: 'app.home', icon: 'pi pi-home', exact: true },
  { to: { name: 'households' }, label: 'app.households', icon: 'pi pi-building' },
  { to: { name: 'account-profile' }, label: 'app.account', icon: 'pi pi-user' },
  ...(session.isAdmin ? [{ to: { name: 'admin-users' }, label: 'app.admin', icon: 'pi pi-users' }] : []),
])
</script>

<template>
  <div class="flex min-h-dvh flex-col">
    <header
      class="sticky top-0 z-10 border-b border-surface-200 bg-surface-0/90 backdrop-blur dark:border-surface-800 dark:bg-surface-900/90"
    >
      <div class="mx-auto flex h-14 max-w-6xl items-center gap-2 px-4 sm:gap-4">
        <RouterLink :to="{ name: 'home' }" class="flex shrink-0 items-center gap-2 font-semibold text-primary">
          <i class="pi pi-home text-xl" aria-hidden="true" />
          <span class="hidden md:inline">{{ t('app.name') }}</span>
        </RouterLink>
        <nav :aria-label="t('app.menu')" class="hidden flex-1 items-center gap-1 overflow-x-auto sm:flex">
          <RouterLink
            v-for="link in links"
            :key="link.label"
            :to="link.to"
            class="flex items-center gap-2 rounded-md px-3 py-2 text-sm whitespace-nowrap text-muted-color hover:bg-emphasis"
            :active-class="link.exact ? '' : '!text-primary bg-highlight'"
            exact-active-class="!text-primary bg-highlight"
          >
            <i :class="link.icon" aria-hidden="true" />
            <span>{{ t(link.label) }}</span>
          </RouterLink>
        </nav>
        <div class="flex min-w-0 flex-1 justify-end sm:flex-none">
          <HouseholdSwitcher />
        </div>
        <div class="flex shrink-0 items-center">
          <ColorSchemeToggle />
          <UserMenu />
        </div>
      </div>
    </header>
    <main
      class="mx-auto w-full max-w-6xl flex-1 px-4 py-6 pb-[calc(5rem+env(safe-area-inset-bottom))] sm:pb-[calc(1.5rem+env(safe-area-inset-bottom))]"
    >
      <!-- Right after sign-up Accounts needs a moment to create the account (user.pending). -->
      <Message v-if="pending" severity="info" :closable="false" class="mb-4">{{ t('errors.user.pending') }}</Message>
      <RouterView />
    </main>
    <!-- On a phone the menu is a tab bar at the bottom, within reach of the thumb (also as an installed PWA). -->
    <nav
      :aria-label="t('app.menu')"
      class="fixed inset-x-0 bottom-0 z-10 flex border-t border-surface-200 bg-surface-0/95 pb-[env(safe-area-inset-bottom)] backdrop-blur sm:hidden dark:border-surface-800 dark:bg-surface-900/95"
    >
      <RouterLink
        v-for="link in links"
        :key="link.label"
        :to="link.to"
        class="flex flex-1 flex-col items-center gap-1 py-2 text-xs text-muted-color"
        :active-class="link.exact ? '' : '!text-primary'"
        exact-active-class="!text-primary"
      >
        <i :class="link.icon" class="text-lg" aria-hidden="true" />
        <span class="max-w-full truncate px-1">{{ t(link.label) }}</span>
      </RouterLink>
    </nav>
  </div>
</template>
