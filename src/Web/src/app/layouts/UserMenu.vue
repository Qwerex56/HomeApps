<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import Menu from 'primevue/menu'
import UserAvatar from '@/shared/components/UserAvatar.vue'
import { useSessionStore } from '@/features/auth/session'
import { useMyAccount } from '@/features/account/api'

const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const { data: account } = useMyAccount()
const menu = ref<InstanceType<typeof Menu>>()

const name = computed(() => account.value?.profile.displayName ?? session.claims?.email ?? '')

const items = computed(() => [
  { label: session.claims?.email, disabled: true },
  { separator: true },
  { label: t('app.account'), icon: 'pi pi-user', command: () => router.push({ name: 'account-profile' }) },
  { label: t('account.tabs.security'), icon: 'pi pi-lock', command: () => router.push({ name: 'account-security' }) },
  { label: t('wall.open'), icon: 'pi pi-tablet', command: () => router.push({ name: 'wall' }) },
  // Administration only for Admin and Owner, right above signing out (2.4.1, U1).
  ...(session.isAdmin
    ? [
        { separator: true },
        { label: t('app.admin'), icon: 'pi pi-users', command: () => router.push({ name: 'admin-users' }) },
      ]
    : []),
  { separator: true },
  { label: t('auth.logout'), icon: 'pi pi-sign-out', command: () => session.signOut() },
  { label: t('auth.logoutEverywhere'), icon: 'pi pi-power-off', command: () => session.signOut(true) },
])
</script>

<template>
  <Button text rounded class="!p-1" :aria-label="t('app.account')" aria-haspopup="true" @click="menu?.toggle($event)">
    <UserAvatar :src="account?.profile.avatarUrl" :name="name" />
    <span class="hidden max-w-40 truncate text-sm md:inline">{{ name }}</span>
  </Button>
  <Menu ref="menu" :model="items" popup />
</template>
