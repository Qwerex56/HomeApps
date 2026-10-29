<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import Menu from 'primevue/menu'
import Avatar from 'primevue/avatar'
import { useSessionStore } from '@/features/auth/session'
import { useMyAccount } from '@/features/account/api'

const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const { data: account } = useMyAccount()
const menu = ref<InstanceType<typeof Menu>>()

const name = computed(() => account.value?.profile.displayName ?? session.claims?.email ?? '')
const initials = computed(() => name.value.slice(0, 1).toUpperCase())

const items = computed(() => [
  { label: session.claims?.email, disabled: true },
  { separator: true },
  { label: t('app.account'), icon: 'pi pi-user', command: () => router.push({ name: 'account-profile' }) },
  { label: t('account.tabs.security'), icon: 'pi pi-lock', command: () => router.push({ name: 'account-security' }) },
  { label: t('wall.open'), icon: 'pi pi-tablet', command: () => router.push({ name: 'wall' }) },
  { separator: true },
  { label: t('auth.logout'), icon: 'pi pi-sign-out', command: () => session.signOut() },
  { label: t('auth.logoutEverywhere'), icon: 'pi pi-power-off', command: () => session.signOut(true) },
])
</script>

<template>
  <Button text rounded class="!p-1" :aria-label="t('app.account')" aria-haspopup="true" @click="menu?.toggle($event)">
    <Avatar v-if="account?.profile.avatarUrl" :image="account.profile.avatarUrl" shape="circle" />
    <Avatar v-else :label="initials" shape="circle" class="!bg-primary !text-primary-contrast" />
    <span class="hidden max-w-40 truncate text-sm md:inline">{{ name }}</span>
  </Button>
  <Menu ref="menu" :model="items" popup />
</template>
