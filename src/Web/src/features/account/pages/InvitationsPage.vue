<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import Skeleton from 'primevue/skeleton'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { securityApi, securityKeys, type Invitation } from '../securityApi'

// D7: every active account can invite people to the app. The email carries the link to /invite/:token.
const { t, d } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()
const { data: invitations, isPending } = useQuery({
  queryKey: securityKeys.invitations,
  queryFn: securityApi.invitations,
})
const { busy, formError, fieldErrors, submit } = useSubmit()
const email = ref('')

const severity: Record<Invitation['status'], string> = {
  Pending: 'info',
  Accepted: 'success',
  Revoked: 'secondary',
  Expired: 'warn',
}

const refresh = () => queryClient.invalidateQueries({ queryKey: securityKeys.invitations })

async function invite() {
  if (await submit(() => securityApi.invite(email.value))) {
    toast.add({ severity: 'success', summary: t('account.invitations.sent', { email: email.value }), life: 4000 })
    email.value = ''
    await refresh()
  }
}

const resend = (item: Invitation) => run(() => securityApi.resendInvitation(item.id), t('account.invitations.resent'))
const revoke = (item: Invitation) => run(() => securityApi.revokeInvitation(item.id), t('account.invitations.revoked'))

async function run(action: () => Promise<void>, done: string) {
  try {
    await action()
    toast.add({ severity: 'success', summary: done, life: 3000 })
    await refresh()
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <Card>
      <template #title>{{ t('account.invitations.title') }}</template>
      <template #subtitle>{{ t('account.invitations.subtitle') }}</template>
      <template #content>
        <form class="flex flex-col gap-3 sm:flex-row sm:items-end" novalidate @submit.prevent="invite">
          <FormField id="inviteEmail" :label="t('auth.fields.email')" :errors="fieldErrors.email" class="flex-1">
            <template #default="{ invalid }">
              <InputText id="inviteEmail" v-model="email" type="email" :invalid="invalid" />
            </template>
          </FormField>
          <Button type="submit" :label="t('account.invitations.send')" icon="pi pi-send" :loading="busy" />
        </form>
        <Message v-if="formError" severity="error" :closable="false" class="mt-3">{{ formError }}</Message>
      </template>
    </Card>

    <Card>
      <template #content>
        <div v-if="isPending" class="flex flex-col gap-3">
          <Skeleton v-for="i in 3" :key="i" height="3rem" />
        </div>
        <p v-else-if="!invitations?.length" class="text-muted-color">{{ t('account.invitations.empty') }}</p>
        <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <li v-for="item in invitations" :key="item.id" class="flex flex-wrap items-center justify-between gap-3 py-3">
            <div class="min-w-0">
              <div class="truncate">{{ item.email }}</div>
              <div class="text-sm text-muted-color">
                {{ t('account.invitations.created', { date: d(new Date(item.createdAt), 'short') }) }} ·
                {{ t('account.invitations.code', { code: item.code }) }}
              </div>
            </div>
            <div class="flex items-center gap-2">
              <Tag :value="t(`account.invitations.status.${item.status}`)" :severity="severity[item.status]" />
              <Button
                v-if="item.status === 'Pending' || item.status === 'Expired'"
                icon="pi pi-refresh"
                :aria-label="t('account.invitations.resend')"
                v-tooltip.top="t('account.invitations.resend')"
                severity="secondary"
                text
                rounded
                @click="resend(item)"
              />
              <Button
                v-if="item.status === 'Pending'"
                icon="pi pi-times"
                :aria-label="t('account.invitations.revoke')"
                v-tooltip.top="t('account.invitations.revoke')"
                severity="danger"
                text
                rounded
                @click="revoke(item)"
              />
            </div>
          </li>
        </ul>
      </template>
    </Card>
  </div>
</template>
