<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Textarea from 'primevue/textarea'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Dialog from 'primevue/dialog'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { useSessionStore } from '@/features/auth/session'
import { accountApi, accountKeys, useMyAccount, type ProfileInput } from '../api'

const { t } = useI18n()
const router = useRouter()
const toast = useToast()
const queryClient = useQueryClient()
const session = useSessionStore()
const { data: account } = useMyAccount()
// Accounts validates the profile as a nested object, so its fields come back as profile.*.
const { busy, formError, fieldErrors, submit } = useSubmit('profile.')

const form = reactive({
  displayName: '',
  firstName: '',
  lastName: '',
  preferredName: '',
  avatarUrl: '',
  locale: 'pl',
  timeZone: 'Europe/Warsaw',
  dateOfBirth: null as Date | null,
})

const languages = [
  { value: 'pl', label: 'Polski' },
  { value: 'en', label: 'English' },
]
// A locale saved elsewhere (e.g. "pl-PL" from an older client) still shows in the list.
const locales = computed(() =>
  languages.some((l) => l.value === form.locale)
    ? languages
    : [...languages, { value: form.locale, label: form.locale }],
)
// Intl.supportedValuesOf is in every current browser but not yet in this TypeScript lib.
const supportedValuesOf = (Intl as { supportedValuesOf?: (key: 'timeZone') => string[] }).supportedValuesOf
const timeZones = supportedValuesOf?.('timeZone') ?? ['Europe/Warsaw', 'UTC']

watch(
  () => account.value?.profile,
  (profile) => {
    if (!profile) return
    Object.assign(form, {
      displayName: profile.displayName,
      firstName: profile.firstName ?? '',
      lastName: profile.lastName ?? '',
      preferredName: profile.preferredName ?? '',
      avatarUrl: profile.avatarUrl ?? '',
      locale: profile.locale,
      timeZone: profile.timeZone,
      dateOfBirth: profile.dateOfBirth ? new Date(`${profile.dateOfBirth}T00:00:00`) : null,
    })
  },
  { immediate: true },
)

function toInput(): ProfileInput {
  const blank = (value: string) => value.trim() || null
  const date = form.dateOfBirth
  return {
    displayName: form.displayName,
    firstName: blank(form.firstName),
    lastName: blank(form.lastName),
    preferredName: blank(form.preferredName),
    avatarUrl: blank(form.avatarUrl),
    locale: form.locale,
    timeZone: form.timeZone,
    // A calendar date, not an instant: no time zone conversion.
    dateOfBirth: date
      ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
      : null,
  }
}

async function save() {
  if (await submit(() => accountApi.updateProfile(toInput()))) {
    await queryClient.invalidateQueries({ queryKey: accountKeys.me })
    toast.add({ severity: 'success', summary: t('account.profile.saved'), life: 3000 })
  }
}

// UC-03: disabling my own account. Signing in again later offers to bring it back (A-09).
const disableOpen = ref(false)
const reason = ref('')
const disabling = useSubmit()

async function disable() {
  if (await disabling.submit(() => accountApi.disable(reason.value.trim() || null))) {
    disableOpen.value = false
    await session.signOut().catch(() => undefined)
    await router.replace({ name: 'login' })
    toast.add({ severity: 'info', summary: t('account.disable.done'), life: 8000 })
  }
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <Card>
      <template #title>{{ t('account.profile.title') }}</template>
      <template #content>
        <form class="flex flex-col gap-4" novalidate @submit.prevent="save">
          <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
          <div class="grid gap-4 sm:grid-cols-2">
            <FormField id="displayName" :label="t('auth.fields.displayName')" :errors="fieldErrors.displayName">
              <template #default="{ invalid }">
                <InputText id="displayName" v-model="form.displayName" :invalid="invalid" />
              </template>
            </FormField>
            <FormField
              id="preferredName"
              :label="t('account.profile.preferredName')"
              :hint="t('account.profile.preferredNameHint')"
              :errors="fieldErrors.preferredName"
            >
              <template #default="{ invalid }">
                <InputText id="preferredName" v-model="form.preferredName" :invalid="invalid" />
              </template>
            </FormField>
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
            <FormField id="dateOfBirth" :label="t('account.profile.dateOfBirth')" :errors="fieldErrors.dateOfBirth">
              <template #default="{ invalid }">
                <DatePicker
                  v-model="form.dateOfBirth"
                  input-id="dateOfBirth"
                  date-format="dd.mm.yy"
                  :max-date="new Date()"
                  show-button-bar
                  fluid
                  :invalid="invalid"
                />
              </template>
            </FormField>
            <FormField id="avatarUrl" :label="t('account.profile.avatarUrl')" :errors="fieldErrors.avatarUrl">
              <template #default="{ invalid }">
                <InputText id="avatarUrl" v-model="form.avatarUrl" type="url" :invalid="invalid" />
              </template>
            </FormField>
            <FormField id="locale" :label="t('account.profile.locale')" :errors="fieldErrors.locale">
              <template #default="{ invalid }">
                <Select
                  v-model="form.locale"
                  input-id="locale"
                  :options="locales"
                  option-label="label"
                  option-value="value"
                  :invalid="invalid"
                />
              </template>
            </FormField>
            <FormField id="timeZone" :label="t('account.profile.timeZone')" :errors="fieldErrors.timeZone">
              <template #default="{ invalid }">
                <Select v-model="form.timeZone" input-id="timeZone" :options="timeZones" filter :invalid="invalid" />
              </template>
            </FormField>
          </div>
          <div class="flex justify-end">
            <Button type="submit" :label="t('common.save')" icon="pi pi-check" :loading="busy" :disabled="!account" />
          </div>
        </form>
      </template>
    </Card>

    <Card class="border border-red-200 dark:border-red-900">
      <template #title>{{ t('account.disable.title') }}</template>
      <template #content>
        <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <p class="text-muted-color">{{ t('account.disable.text') }}</p>
          <Button :label="t('account.disable.button')" severity="danger" outlined @click="disableOpen = true" />
        </div>
      </template>
    </Card>

    <Dialog v-model:visible="disableOpen" modal :header="t('account.disable.confirmTitle')" class="w-full max-w-md">
      <div class="flex flex-col gap-4">
        <p>{{ t('account.disable.confirmText') }}</p>
        <Message v-if="disabling.formError.value" severity="error" :closable="false">{{
          disabling.formError.value
        }}</Message>
        <FormField id="reason" :label="t('account.disable.reason')">
          <Textarea id="reason" v-model="reason" rows="3" auto-resize />
        </FormField>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" severity="secondary" text @click="disableOpen = false" />
        <Button
          :label="t('account.disable.button')"
          severity="danger"
          :loading="disabling.busy.value"
          @click="disable"
        />
      </template>
    </Dialog>
  </div>
</template>
