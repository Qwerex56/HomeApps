<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { authApi } from '../api'
import { useSessionStore } from '../session'

// A-08: the link from the "forgot password" email (/reset-password?token=, fixed by Auth's email templates).
const { t } = useI18n()
const route = useRoute()
const session = useSessionStore()
const { busy, formError, fieldErrors, submit } = useSubmit()
const token = typeof route.query.token === 'string' ? route.query.token : ''
const password = ref('')
const done = ref(false)

async function reset() {
  done.value = await submit(() => authApi.resetPassword(token, password.value))
  if (done.value) {
    // A new password ends every session, this one included.
    session.clear()
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('password.reset.title') }}</template>
    <template #content>
      <Message v-if="!token" severity="error" :closable="false">{{ t('errors.token.invalid_or_expired') }}</Message>
      <div v-else-if="done" class="flex flex-col gap-4">
        <Message severity="success" :closable="false">{{ t('password.reset.done') }}</Message>
        <RouterLink :to="{ name: 'login' }" class="text-primary hover:underline">{{
          t('auth.login.title')
        }}</RouterLink>
      </div>
      <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="reset">
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <FormField id="password" :label="t('password.reset.new')" :errors="fieldErrors.password">
          <template #default="{ invalid }">
            <Password
              v-model="password"
              input-id="password"
              :feedback="false"
              toggle-mask
              fluid
              :input-props="{ autocomplete: 'new-password' }"
              :invalid="invalid"
              autofocus
            />
          </template>
        </FormField>
        <Button type="submit" :label="t('password.reset.submit')" :loading="busy" class="w-full" />
      </form>
    </template>
  </Card>
</template>
