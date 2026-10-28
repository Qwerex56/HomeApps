<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { authApi } from '../api'
import { useSessionStore } from '../session'

// A-15: Google returned an email that already has an account. Linking needs that account's password; "No" changes nothing.
const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const { busy, formError, fieldErrors, submit } = useSubmit()

const { ticket, email } = (history.state ?? {}) as { ticket?: string; email?: string }
const password = ref('')

async function link() {
  if (await submit(async () => session.apply(await authApi.confirmLink(ticket!, password.value)))) {
    await router.replace('/')
  }
}

async function decline() {
  await authApi.declineLink(ticket!).catch(() => undefined)
  await router.replace({ name: 'login' })
}
</script>

<template>
  <Card>
    <template #title>{{ t('auth.link.title') }}</template>
    <template #content>
      <Message v-if="!ticket" severity="warn" :closable="false">{{ t('auth.ticketMissing') }}</Message>
      <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="link">
        <p class="text-muted-color">{{ t('auth.link.text', { email }) }}</p>
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <FormField id="password" :label="t('auth.fields.password')" :errors="fieldErrors.password">
          <template #default="{ invalid }">
            <Password
              v-model="password"
              input-id="password"
              :feedback="false"
              toggle-mask
              fluid
              :input-props="{ autocomplete: 'current-password' }"
              :invalid="invalid"
              autofocus
            />
          </template>
        </FormField>
        <div class="flex flex-col gap-2 sm:flex-row-reverse">
          <Button type="submit" :label="t('auth.link.confirm')" :loading="busy" class="flex-1" />
          <Button :label="t('auth.link.decline')" severity="secondary" text class="flex-1" @click="decline" />
        </div>
      </form>
    </template>
  </Card>
</template>
