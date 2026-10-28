<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import ProgressSpinner from 'primevue/progressspinner'
import Message from 'primevue/message'
import { errorMessage } from '@/api/errorMessage'
import { useSessionStore } from '../session'

// A-05: Auth sends the browser back here after Google, with the result in the fragment (never in a server log).
const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const error = ref<string | null>(null)

onMounted(async () => {
  const result = new URLSearchParams(window.location.hash.slice(1))
  // The ticket must not stay in the address bar or the history.
  history.replaceState(history.state, '', window.location.pathname)

  const code = result.get('error')
  const ticket = result.get('ticket')
  if (code === 'account.disabled_by_user' && ticket) {
    await router.replace({ name: 'reactivate', state: { ticket } })
  } else if (code === 'external_login.link_required' && ticket) {
    await router.replace({
      name: 'link-account',
      state: { ticket, email: result.get('email') ?? '', provider: result.get('provider') ?? 'google' },
    })
  } else if (code) {
    error.value = errorMessage({ status: 400, code, errors: [], fieldErrors: {} })
  } else if (await session.refresh()) {
    // Signed in: Auth set the refresh cookie, the access token comes from /auth/refresh.
    await router.replace('/')
  } else {
    error.value = t('errors.external_login.failed')
  }
})
</script>

<template>
  <Card>
    <template #content>
      <div v-if="!error" class="flex flex-col items-center gap-3 py-6">
        <ProgressSpinner style="width: 2.5rem; height: 2.5rem" />
        <span class="text-muted-color">{{ t('auth.callback.working') }}</span>
      </div>
      <div v-else class="flex flex-col gap-4">
        <Message severity="error" :closable="false">{{ error }}</Message>
        <RouterLink :to="{ name: 'login' }" class="text-primary hover:underline">{{
          t('auth.callback.back')
        }}</RouterLink>
      </div>
    </template>
  </Card>
</template>
