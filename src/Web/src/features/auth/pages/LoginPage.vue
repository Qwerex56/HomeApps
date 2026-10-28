<script setup lang="ts">
import { reactive } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Divider from 'primevue/divider'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { authApi } from '../api'
import { useSessionStore } from '../session'
import { useAfterSignIn } from '../useAfterSignIn'
import GoogleButton from '../GoogleButton.vue'

const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const afterSignIn = useAfterSignIn()
const { busy, formError, fieldErrors, submit } = useSubmit()

const form = reactive({ email: '', password: '' })

async function signIn() {
  const ok = await submit(
    async () => session.apply(await authApi.login(form.email, form.password)),
    (problem) => {
      // A-09: the user disabled this account; the ticket lets them bring it back.
      if (problem.code === 'account.disabled_by_user' && problem.ticket) {
        void router.push({ name: 'reactivate', state: { ticket: problem.ticket } })
        return true
      }
    },
  )
  if (ok) {
    await afterSignIn()
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('auth.login.title') }}</template>
    <template #content>
      <form class="flex flex-col gap-4" novalidate @submit.prevent="signIn">
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <FormField id="email" :label="t('auth.fields.email')" :errors="fieldErrors.email">
          <template #default="{ invalid }">
            <InputText
              id="email"
              v-model="form.email"
              type="email"
              autocomplete="username"
              :invalid="invalid"
              autofocus
            />
          </template>
        </FormField>
        <FormField id="password" :label="t('auth.fields.password')" :errors="fieldErrors.password">
          <template #aside>
            <RouterLink :to="{ name: 'forgot-password' }" class="text-sm text-primary hover:underline">
              {{ t('auth.login.forgot') }}
            </RouterLink>
          </template>
          <template #default="{ invalid }">
            <Password
              v-model="form.password"
              input-id="password"
              :feedback="false"
              toggle-mask
              fluid
              :input-props="{ autocomplete: 'current-password' }"
              :invalid="invalid"
            />
          </template>
        </FormField>
        <Button type="submit" :label="t('auth.login.submit')" :loading="busy" class="w-full" />
      </form>
      <Divider align="center"
        ><span class="text-sm text-muted-color">{{ t('auth.or') }}</span></Divider
      >
      <GoogleButton />
      <p class="mt-6 text-center text-sm text-muted-color">
        {{ t('auth.login.noAccount') }}
        <RouterLink :to="{ name: 'register' }" class="text-primary hover:underline">{{
          t('auth.login.register')
        }}</RouterLink>
      </p>
    </template>
  </Card>
</template>
