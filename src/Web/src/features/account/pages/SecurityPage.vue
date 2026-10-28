<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem, type Problem } from '@/api/problem'
import { googleSignInUrl } from '@/features/auth/api'
import { securityApi, securityKeys } from '../securityApi'

const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const { data: me } = useQuery({ queryKey: securityKeys.me, queryFn: securityApi.me })
const google = computed(() => me.value?.logins.find((login) => login.provider === 'Google'))

// R8: a change on an account without a password needs a fresh sign-in (Google) from the last 5 minutes.
const needsReauth = ref(false)
function reauth(problem: Problem) {
  if (problem.code === 'auth.reauthentication_required') {
    needsReauth.value = true
    return true
  }
}

// Auth answers a wrong current password with the sign-in error; here it belongs under the password field.
function wrongPassword(form: ReturnType<typeof useSubmit>) {
  return (problem: Problem) => {
    if (problem.code === 'auth.invalid_credentials') {
      form.fieldErrors.value = { currentPassword: [t('account.security.wrongPassword')] }
      return true
    }
    return reauth(problem)
  }
}

// Changing the password: it starts working after the link in the email (/confirm-password), which signs out everywhere.
const password = reactive({ current: '', next: '' })
const passwordSent = ref(false)
const passwordForm = useSubmit()
async function changePassword() {
  passwordSent.value = await passwordForm.submit(
    () => securityApi.changePassword(me.value?.hasPassword ? password.current : null, password.next),
    wrongPassword(passwordForm),
  )
  if (passwordSent.value) Object.assign(password, { current: '', next: '' })
}

// Changing the email: confirmed from the new address (/confirm-email).
const email = reactive({ next: '', password: '' })
const emailSent = ref(false)
const emailForm = useSubmit()
async function changeEmail() {
  emailSent.value = await emailForm.submit(
    () => securityApi.changeEmail(email.next, me.value?.hasPassword ? email.password : null),
    wrongPassword(emailForm),
  )
}

async function linkGoogle() {
  try {
    window.location.assign(await securityApi.googleLinkUrl('/account/security'))
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}

function unlinkGoogle() {
  confirm.require({
    header: t('account.security.unlinkTitle'),
    message: t('account.security.unlinkText'),
    acceptProps: { label: t('account.security.unlink'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: async () => {
      try {
        await securityApi.unlink('google')
        await queryClient.invalidateQueries({ queryKey: securityKeys.me })
      } catch (error) {
        const problem = toProblem(error)
        if (!reauth(problem)) toast.add({ severity: 'error', summary: errorMessage(problem), life: 6000 })
      }
    },
  })
}

// A-11: Auth comes back here after linking Google, with the result in the fragment.
onMounted(() => {
  const result = new URLSearchParams(window.location.hash.slice(1))
  if (!result.size) return
  history.replaceState(history.state, '', window.location.pathname)
  if (result.get('linked')) {
    toast.add({ severity: 'success', summary: t('account.security.linked'), life: 4000 })
  } else if (result.get('error')) {
    const code = result.get('error')!
    toast.add({
      severity: 'error',
      summary: errorMessage({ status: 400, code, errors: [], fieldErrors: {} }),
      life: 6000,
    })
  }
})
</script>

<template>
  <div class="flex flex-col gap-6">
    <Message v-if="needsReauth" severity="warn" :closable="false">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <span>{{ t('account.security.reauth') }}</span>
        <a :href="googleSignInUrl()" class="font-medium text-primary hover:underline">{{ t('auth.google') }}</a>
      </div>
    </Message>

    <Card>
      <template #title>{{ t('account.security.methods') }}</template>
      <template #content>
        <ul class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <li class="flex items-center justify-between gap-4 py-3">
            <div class="flex items-center gap-3">
              <i class="pi pi-key text-muted-color" aria-hidden="true" />
              <span>{{ t('auth.fields.password') }}</span>
            </div>
            <Tag
              :value="me?.hasPassword ? t('account.security.set') : t('account.security.notSet')"
              :severity="me?.hasPassword ? 'success' : 'secondary'"
            />
          </li>
          <li class="flex items-center justify-between gap-4 py-3">
            <div class="flex min-w-0 items-center gap-3">
              <i class="pi pi-google text-muted-color" aria-hidden="true" />
              <div class="min-w-0">
                <div>Google</div>
                <div v-if="google" class="truncate text-sm text-muted-color">
                  {{ google.email }} · {{ d(new Date(google.linkedAt), 'short') }}
                </div>
              </div>
            </div>
            <Button
              v-if="google"
              :label="t('account.security.unlink')"
              severity="secondary"
              text
              size="small"
              @click="unlinkGoogle"
            />
            <Button
              v-else
              :label="t('account.security.link')"
              severity="secondary"
              outlined
              size="small"
              @click="linkGoogle"
            />
          </li>
        </ul>
      </template>
    </Card>

    <Card>
      <template #title>{{
        me?.hasPassword ? t('account.security.changePassword') : t('account.security.addPassword')
      }}</template>
      <template #content>
        <Message v-if="passwordSent" severity="success" :closable="false">{{
          t('account.security.passwordSent')
        }}</Message>
        <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="changePassword">
          <Message v-if="passwordForm.formError.value" severity="error" :closable="false">
            {{ passwordForm.formError.value }}
          </Message>
          <FormField
            v-if="me?.hasPassword"
            id="currentPassword"
            :label="t('account.security.currentPassword')"
            :errors="passwordForm.fieldErrors.value.currentPassword"
          >
            <template #default="{ invalid }">
              <Password
                v-model="password.current"
                input-id="currentPassword"
                :feedback="false"
                toggle-mask
                fluid
                :input-props="{ autocomplete: 'current-password' }"
                :invalid="invalid"
              />
            </template>
          </FormField>
          <FormField
            id="newPassword"
            :label="t('password.reset.new')"
            :errors="passwordForm.fieldErrors.value.newPassword"
          >
            <template #default="{ invalid }">
              <Password
                v-model="password.next"
                input-id="newPassword"
                :feedback="false"
                toggle-mask
                fluid
                :input-props="{ autocomplete: 'new-password' }"
                :invalid="invalid"
              />
            </template>
          </FormField>
          <div class="flex justify-end">
            <Button type="submit" :label="t('account.security.sendLink')" :loading="passwordForm.busy.value" />
          </div>
        </form>
      </template>
    </Card>

    <Card>
      <template #title>{{ t('account.security.changeEmail') }}</template>
      <template #content>
        <Message v-if="emailSent" severity="success" :closable="false">
          {{ t('account.security.emailSent', { email: email.next }) }}
        </Message>
        <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="changeEmail">
          <p class="text-muted-color">{{ t('account.security.currentEmail', { email: me?.email }) }}</p>
          <Message v-if="emailForm.formError.value" severity="error" :closable="false">{{
            emailForm.formError.value
          }}</Message>
          <FormField
            id="newEmail"
            :label="t('account.security.newEmail')"
            :errors="emailForm.fieldErrors.value.newEmail"
          >
            <template #default="{ invalid }">
              <InputText id="newEmail" v-model="email.next" type="email" autocomplete="email" :invalid="invalid" />
            </template>
          </FormField>
          <FormField
            v-if="me?.hasPassword"
            id="emailPassword"
            :label="t('account.security.currentPassword')"
            :errors="emailForm.fieldErrors.value.currentPassword"
          >
            <template #default="{ invalid }">
              <Password
                v-model="email.password"
                input-id="emailPassword"
                :feedback="false"
                toggle-mask
                fluid
                :input-props="{ autocomplete: 'current-password' }"
                :invalid="invalid"
              />
            </template>
          </FormField>
          <div class="flex justify-end">
            <Button type="submit" :label="t('account.security.sendLink')" :loading="emailForm.busy.value" />
          </div>
        </form>
      </template>
    </Card>
  </div>
</template>
