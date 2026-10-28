<script setup lang="ts">
import { reactive } from 'vue'
import { RouterLink } from 'vue-router'
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

// Open sign-up (Auth:RegistrationOpen); with it off Auth answers auth.registration_closed and only invitations work (F3).
const { t } = useI18n()
const session = useSessionStore()
const afterSignIn = useAfterSignIn()
const { busy, formError, fieldErrors, submit } = useSubmit()

const form = reactive({ displayName: '', email: '', password: '', firstName: '', lastName: '' })

async function register() {
  const ok = await submit(async () =>
    session.apply(
      await authApi.register({
        email: form.email,
        password: form.password,
        displayName: form.displayName,
        firstName: form.firstName || null,
        lastName: form.lastName || null,
        locale: 'pl',
      }),
    ),
  )
  if (ok) {
    await afterSignIn()
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('auth.register.title') }}</template>
    <template #content>
      <form class="flex flex-col gap-4" novalidate @submit.prevent="register">
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <FormField id="displayName" :label="t('auth.fields.displayName')" :errors="fieldErrors.displayName">
          <template #default="{ invalid }">
            <InputText
              id="displayName"
              v-model="form.displayName"
              autocomplete="nickname"
              :invalid="invalid"
              autofocus
            />
          </template>
        </FormField>
        <div class="grid gap-4 sm:grid-cols-2">
          <FormField id="firstName" :label="t('auth.fields.firstName')" :errors="fieldErrors.firstName">
            <template #default="{ invalid }">
              <InputText id="firstName" v-model="form.firstName" autocomplete="given-name" :invalid="invalid" />
            </template>
          </FormField>
          <FormField id="lastName" :label="t('auth.fields.lastName')" :errors="fieldErrors.lastName">
            <template #default="{ invalid }">
              <InputText id="lastName" v-model="form.lastName" autocomplete="family-name" :invalid="invalid" />
            </template>
          </FormField>
        </div>
        <FormField id="email" :label="t('auth.fields.email')" :errors="fieldErrors.email">
          <template #default="{ invalid }">
            <InputText id="email" v-model="form.email" type="email" autocomplete="email" :invalid="invalid" />
          </template>
        </FormField>
        <FormField id="password" :label="t('auth.fields.password')" :errors="fieldErrors.password">
          <template #default="{ invalid }">
            <Password
              v-model="form.password"
              input-id="password"
              :feedback="false"
              toggle-mask
              fluid
              :input-props="{ autocomplete: 'new-password' }"
              :invalid="invalid"
            />
          </template>
        </FormField>
        <Button type="submit" :label="t('auth.register.submit')" :loading="busy" class="w-full" />
      </form>
      <Divider align="center"
        ><span class="text-sm text-muted-color">{{ t('auth.or') }}</span></Divider
      >
      <GoogleButton />
      <p class="mt-6 text-center text-sm text-muted-color">
        {{ t('auth.register.haveAccount') }}
        <RouterLink :to="{ name: 'login' }" class="text-primary hover:underline">{{
          t('auth.register.login')
        }}</RouterLink>
      </p>
    </template>
  </Card>
</template>
