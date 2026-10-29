<script setup lang="ts">
import { computed, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { useSubmit } from '@/shared/composables/useSubmit'
import { accountKeys } from '@/features/account/api'
import { authApi } from '../api'
import { useSessionStore } from '../session'

// The links from the "confirm your new password" and "confirm your new email" emails (/confirm-password, /confirm-email).
// A button, not an automatic request: mail scanners open links, and a click is what proves the person meant it.
const props = defineProps<{ kind: 'password' | 'email' }>()

const { t } = useI18n()
const route = useRoute()
const session = useSessionStore()
const queryClient = useQueryClient()
const { busy, formError, submit } = useSubmit()
const token = typeof route.query.token === 'string' ? route.query.token : ''
const done = ref(false)
const texts = computed(() => `confirm.${props.kind}`)

async function confirm() {
  if (props.kind === 'password') {
    done.value = await submit(() => authApi.confirmPasswordChange(token))
    // The new password signs out every session (Q1).
    if (done.value) session.clear()
  } else {
    done.value = await submit(() => authApi.confirmEmailChange(token))
    // The token carries the email: refresh it and the account when signed in here.
    if (done.value && session.status === 'signedIn') {
      await session.refresh()
      await queryClient.invalidateQueries({ queryKey: accountKeys.me })
    }
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t(`${texts}.title`) }}</template>
    <template #content>
      <Message v-if="!token" severity="error" :closable="false">{{ t('errors.token.invalid_or_expired') }}</Message>
      <div v-else-if="done" class="flex flex-col gap-4">
        <Message severity="success" :closable="false">{{ t(`${texts}.done`) }}</Message>
        <RouterLink :to="session.status === 'signedIn' ? '/' : { name: 'login' }" class="text-primary hover:underline">
          {{ session.status === 'signedIn' ? t('app.backHome') : t('auth.login.title') }}
        </RouterLink>
      </div>
      <div v-else class="flex flex-col gap-4">
        <p class="text-muted-color">{{ t(`${texts}.text`) }}</p>
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <Button :label="t(`${texts}.submit`)" :loading="busy" class="w-full" @click="confirm" />
      </div>
    </template>
  </Card>
</template>
