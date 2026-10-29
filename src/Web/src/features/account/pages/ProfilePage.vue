<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import Textarea from 'primevue/textarea'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Dialog from 'primevue/dialog'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { useSessionStore } from '@/features/auth/session'
import ProfileForm from '../components/ProfileForm.vue'
import { accountApi, accountKeys, useMyAccount } from '../api'

const { t } = useI18n()
const router = useRouter()
const toast = useToast()
const queryClient = useQueryClient()
const session = useSessionStore()
const { data: account } = useMyAccount()

async function saved() {
  await queryClient.invalidateQueries({ queryKey: accountKeys.me })
  toast.add({ severity: 'success', summary: t('account.profile.saved'), life: 3000 })
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
        <ProfileForm :profile="account?.profile" :save="accountApi.updateProfile" @saved="saved" />
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
