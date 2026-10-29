<script setup lang="ts">
import { computed, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import {
  VALIDITIES,
  formatCode,
  householdKeys,
  householdsApi,
  joinUrl,
  type Household,
  type HouseholdPermissions,
  type Invite,
  type InviteValidity,
} from '../api'
import { roleSeverity } from '../tags'

// I1–I3 and the guest link (D2b, D3): personal invites for Member or Guest, valid 30 minutes to 7 days.
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const id = computed(() => props.household.id)

const { data: invites, isPending } = useQuery({
  queryKey: computed(() => householdKeys.invites(id.value)),
  queryFn: () => householdsApi.invites(id.value),
})

const form = reactive({
  role: 'Member' as 'Member' | 'Guest',
  validity: 'SevenDays' as InviteValidity,
  guestAccessUntil: null as Date | null,
})
const roles = computed(() =>
  (['Member', 'Guest'] as const).map((role) => ({ value: role, label: t(`households.role.${role}`) })),
)
const validities = computed(() => VALIDITIES.map((v) => ({ value: v, label: t(`households.invites.validity.${v}`) })))
const { busy, formError, fieldErrors, submit } = useSubmit()

async function create() {
  const until = form.role === 'Guest' && form.guestAccessUntil ? form.guestAccessUntil.toISOString() : null
  if (await submit(() => householdsApi.invite(id.value, form.role, form.validity, until))) {
    toast.add({ severity: 'success', summary: t('households.invites.created'), life: 3000 })
    await queryClient.invalidateQueries({ queryKey: householdKeys.invites(id.value) })
  }
}

async function copy(code: string) {
  const url = joinUrl(code)
  try {
    await navigator.clipboard.writeText(url)
    toast.add({ severity: 'success', summary: t('households.invites.copied'), life: 2500 })
  } catch {
    // No clipboard (http, old browser): show the link so it can be copied by hand.
    toast.add({ severity: 'info', summary: url, life: 15000 })
  }
}

async function share(code: string) {
  const url = joinUrl(code)
  if (navigator.share) {
    await navigator.share({ title: props.household.name, url }).catch(() => undefined)
  } else {
    await copy(code)
  }
}

async function run(action: () => Promise<unknown>, message: string) {
  try {
    await action()
    toast.add({ severity: 'success', summary: message, life: 3000 })
    await queryClient.invalidateQueries({ queryKey: householdKeys.all })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}

const revoke = (invite: Invite) =>
  run(() => householdsApi.revokeInvite(id.value, invite.id), t('households.invites.revoked'))

function newGuestLink() {
  const replace = () => run(() => householdsApi.newGuestLink(id.value), t('households.guestLink.created'))
  if (!props.household.guestLink) return replace()
  confirm.require({
    header: t('households.guestLink.regenerateTitle'),
    message: t('households.guestLink.regenerateText'),
    acceptProps: { label: t('households.guestLink.regenerate') },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: replace,
  })
}

const disableGuestLink = () => run(() => householdsApi.disableGuestLink(id.value), t('households.guestLink.disabled'))
</script>

<template>
  <div class="flex flex-col gap-6">
    <Card>
      <template #title>{{ t('households.invites.title') }}</template>
      <template #subtitle>{{ t('households.invites.subtitle') }}</template>
      <template #content>
        <form class="flex flex-col gap-4" novalidate @submit.prevent="create">
          <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
          <div class="grid gap-4 sm:grid-cols-3">
            <FormField id="inviteRole" :label="t('households.invites.role')" :errors="fieldErrors.role">
              <Select
                v-model="form.role"
                input-id="inviteRole"
                :options="roles"
                option-label="label"
                option-value="value"
              />
            </FormField>
            <FormField id="inviteValidity" :label="t('households.invites.valid')" :errors="fieldErrors.validity">
              <Select
                v-model="form.validity"
                input-id="inviteValidity"
                :options="validities"
                option-label="label"
                option-value="value"
              />
            </FormField>
            <FormField
              v-if="form.role === 'Guest'"
              id="guestAccessUntil"
              :label="t('households.invites.guestUntil')"
              :errors="fieldErrors.guestAccessUntil"
            >
              <DatePicker
                v-model="form.guestAccessUntil"
                input-id="guestAccessUntil"
                show-time
                hour-format="24"
                :min-date="new Date()"
                show-button-bar
                fluid
              />
            </FormField>
          </div>
          <div class="flex justify-end">
            <Button type="submit" :label="t('households.invites.create')" icon="pi pi-plus" :loading="busy" />
          </div>
        </form>

        <div v-if="isPending" class="mt-4 flex flex-col gap-3">
          <Skeleton v-for="i in 2" :key="i" height="3rem" />
        </div>
        <p v-else-if="!invites?.length" class="mt-4 text-muted-color">{{ t('households.invites.empty') }}</p>
        <ul v-else class="mt-4 flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <li v-for="invite in invites" :key="invite.id" class="flex flex-wrap items-center justify-between gap-3 py-3">
            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <span class="font-mono text-lg">{{ formatCode(invite.code) }}</span>
                <Tag :value="t(`households.role.${invite.role}`)" :severity="roleSeverity[invite.role]" />
              </div>
              <div class="text-sm text-muted-color">
                {{ t('households.invites.expires', { date: d(new Date(invite.expiresAt), 'long') }) }} ·
                {{ t('households.invites.uses', Number(invite.uses)) }}
                <span v-if="invite.createdByName"> · {{ invite.createdByName }}</span>
              </div>
            </div>
            <div class="flex gap-1">
              <Button
                icon="pi pi-copy"
                severity="secondary"
                text
                rounded
                :aria-label="t('households.invites.copy')"
                v-tooltip.top="t('households.invites.copy')"
                @click="copy(invite.code)"
              />
              <Button
                icon="pi pi-share-alt"
                severity="secondary"
                text
                rounded
                :aria-label="t('households.invites.share')"
                v-tooltip.top="t('households.invites.share')"
                @click="share(invite.code)"
              />
              <Button
                icon="pi pi-times"
                severity="danger"
                text
                rounded
                :aria-label="t('households.invites.revoke')"
                v-tooltip.top="t('households.invites.revoke')"
                @click="revoke(invite)"
              />
            </div>
          </li>
        </ul>
      </template>
    </Card>

    <Card v-if="can.guestLink">
      <template #title>{{ t('households.guestLink.title') }}</template>
      <template #subtitle>{{ t('households.guestLink.subtitle') }}</template>
      <template #content>
        <div v-if="household.guestLink" class="flex flex-col gap-3">
          <div class="flex flex-wrap items-center gap-2">
            <span class="font-mono text-lg">{{ formatCode(household.guestLink) }}</span>
            <Button
              icon="pi pi-copy"
              :label="t('households.invites.copy')"
              severity="secondary"
              text
              @click="copy(household.guestLink)"
            />
          </div>
          <div class="flex flex-wrap gap-2">
            <Button
              :label="t('households.guestLink.regenerate')"
              icon="pi pi-refresh"
              severity="secondary"
              outlined
              @click="newGuestLink"
            />
            <Button
              :label="t('households.guestLink.disable')"
              icon="pi pi-ban"
              severity="danger"
              outlined
              @click="disableGuestLink"
            />
          </div>
        </div>
        <div v-else class="flex flex-wrap items-center justify-between gap-3">
          <p class="text-muted-color">{{ t('households.guestLink.none') }}</p>
          <Button :label="t('households.guestLink.create')" icon="pi pi-link" @click="newGuestLink" />
        </div>
      </template>
    </Card>
  </div>
</template>
