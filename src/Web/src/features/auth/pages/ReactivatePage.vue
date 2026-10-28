<script setup lang="ts">
import { useAfterSignIn } from '../useAfterSignIn'
import { onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { reactivationApi, type Profile } from '@/features/account/api'
import { authApi } from '../api'
import { useSessionStore } from '../session'

// A-09 / UC-05: "Are you sure you want to restore your account?" with the current profile to check, then the ticket
// becomes a session. Auth learns about it from Accounts' UserEnabled, so completing waits on 409 reactivation.pending.
const { t } = useI18n()
const afterSignIn = useAfterSignIn()
const session = useSessionStore()
const { busy, formError, fieldErrors, submit } = useSubmit()

const ticket = (history.state as { ticket?: string } | null)?.ticket
const profile = ref<Profile | null>(null)
const loadError = ref<string | null>(ticket ? null : t('auth.ticketMissing'))
const form = reactive({ displayName: '', firstName: '', lastName: '' })
const waiting = ref(false)

onMounted(async () => {
  if (!ticket) return
  try {
    profile.value = await reactivationApi.form(ticket)
    form.displayName = profile.value.displayName
    form.firstName = profile.value.firstName ?? ''
    form.lastName = profile.value.lastName ?? ''
  } catch (error) {
    loadError.value = errorMessage(toProblem(error))
  }
})

async function reactivate() {
  const ok = await submit(async () => {
    await reactivationApi.reactivate(ticket!, {
      ...profile.value!,
      displayName: form.displayName,
      firstName: form.firstName || null,
      lastName: form.lastName || null,
    })
    waiting.value = true
    session.apply(await completeWhenReady(ticket!))
  })
  waiting.value = false
  if (ok) {
    await afterSignIn()
  }
}

async function completeWhenReady(ticket: string, attempts = 10) {
  for (let attempt = 1; ; attempt++) {
    try {
      return await authApi.completeReactivation(ticket)
    } catch (error) {
      const problem = toProblem(error)
      if (problem.code !== 'reactivation.pending' || attempt >= attempts) throw error
      await new Promise((resolve) => setTimeout(resolve, (problem.retryAfter ?? 1) * 1000))
    }
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('auth.reactivate.title') }}</template>
    <template #content>
      <div v-if="loadError" class="flex flex-col gap-4">
        <Message severity="error" :closable="false">{{ loadError }}</Message>
        <RouterLink :to="{ name: 'login' }" class="text-primary hover:underline">{{
          t('auth.callback.back')
        }}</RouterLink>
      </div>
      <div v-else-if="!profile" class="flex justify-center py-6">
        <ProgressSpinner style="width: 2.5rem; height: 2.5rem" />
      </div>
      <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="reactivate">
        <p class="text-muted-color">{{ t('auth.reactivate.text') }}</p>
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <FormField id="displayName" :label="t('auth.fields.displayName')" :errors="fieldErrors.displayName">
          <template #default="{ invalid }">
            <InputText id="displayName" v-model="form.displayName" :invalid="invalid" />
          </template>
        </FormField>
        <div class="grid gap-4 sm:grid-cols-2">
          <FormField id="firstName" :label="t('auth.fields.firstName')" :errors="fieldErrors.firstName">
            <template #default="{ invalid }">
              <InputText id="firstName" v-model="form.firstName" :invalid="invalid" />
            </template>
          </FormField>
          <FormField id="lastName" :label="t('auth.fields.lastName')" :errors="fieldErrors.lastName">
            <template #default="{ invalid }">
              <InputText id="lastName" v-model="form.lastName" :invalid="invalid" />
            </template>
          </FormField>
        </div>
        <Message v-if="waiting" severity="info" :closable="false">{{ t('errors.reactivation.pending') }}</Message>
        <div class="flex flex-col gap-2 sm:flex-row-reverse">
          <Button type="submit" :label="t('auth.reactivate.confirm')" :loading="busy" class="flex-1" />
          <RouterLink :to="{ name: 'login' }" class="flex-1">
            <Button :label="t('auth.reactivate.cancel')" severity="secondary" text class="w-full" />
          </RouterLink>
        </div>
      </form>
    </template>
  </Card>
</template>
