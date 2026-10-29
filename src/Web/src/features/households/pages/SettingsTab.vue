<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Button from 'primevue/button'
import Message from 'primevue/message'
import FormField from '@/shared/components/FormField.vue'
import PluginsSettings from '@/features/plugins/PluginsSettings.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { householdKeys, householdsApi, type Household, type HouseholdPermissions } from '../api'

// My membership (nickname, leaving) for everyone; name and description for Owner and Admins; archive for the Owner.
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t, d } = useI18n()
const router = useRouter()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const id = computed(() => props.household.id)

const { data: me } = useQuery({
  queryKey: computed(() => householdKeys.me(id.value)),
  queryFn: () => householdsApi.myMembership(id.value),
  enabled: computed(() => props.can.member),
})

const details = reactive({ name: '', description: '' })
watch(
  () => props.household,
  (household) => Object.assign(details, { name: household.name, description: household.description ?? '' }),
  { immediate: true },
)
const detailsForm = useSubmit()
async function saveDetails() {
  if (
    await detailsForm.submit(() => householdsApi.update(id.value, details.name, details.description.trim() || null))
  ) {
    toast.add({ severity: 'success', summary: t('households.settings.saved'), life: 3000 })
    await queryClient.invalidateQueries({ queryKey: householdKeys.all })
  }
}

// My nickname here (M2). Guests cannot set one (Member and up).
const nickname = ref('')
watch(me, (value) => (nickname.value = value?.nickname ?? ''), { immediate: true })
const nicknameForm = useSubmit()
async function saveNickname() {
  if (await nicknameForm.submit(() => householdsApi.setMyNickname(id.value, nickname.value.trim() || null))) {
    toast.add({ severity: 'success', summary: t('households.settings.saved'), life: 3000 })
    await queryClient.invalidateQueries({ queryKey: householdKeys.all })
  }
}

async function goAway(action: () => Promise<void>, message: string) {
  try {
    await action()
    toast.add({ severity: 'success', summary: message, life: 4000 })
    await queryClient.invalidateQueries({ queryKey: householdKeys.all })
    await router.replace({ name: 'households' })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}

function leave() {
  confirm.require({
    header: t('households.settings.leaveTitle'),
    message: t('households.settings.leaveText', { name: props.household.name }),
    acceptProps: { label: t('households.settings.leave'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => goAway(() => householdsApi.leave(id.value), t('households.settings.left')),
  })
}

function archive() {
  confirm.require({
    header: t('households.settings.archiveTitle'),
    message: t('households.settings.archiveText', { name: props.household.name }),
    acceptProps: { label: t('households.settings.archive'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => goAway(() => householdsApi.archive(id.value), t('households.settings.archived')),
  })
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <Card v-if="can.member">
      <template #title>{{ t('households.settings.membership') }}</template>
      <template #content>
        <div class="flex flex-col gap-4">
          <p v-if="me" class="text-muted-color">
            {{ t('households.members.joined', { date: d(new Date(me.joinedAt), 'long') }) }}
            <span v-if="me.accessUntil">
              · {{ t('households.members.until', { date: d(new Date(me.accessUntil), 'long') }) }}
            </span>
          </p>
          <Message v-if="!can.seeMembers" severity="info" :closable="false">{{
            t('households.settings.guestInfo')
          }}</Message>
          <form
            v-if="can.seeMembers"
            class="flex flex-col gap-3 sm:flex-row sm:items-end"
            @submit.prevent="saveNickname"
          >
            <FormField
              id="myNickname"
              :label="t('households.nickname')"
              :hint="t('households.settings.nicknameHint')"
              :errors="nicknameForm.fieldErrors.value.nickname"
              class="flex-1"
            >
              <InputText id="myNickname" v-model="nickname" />
            </FormField>
            <Button type="submit" :label="t('common.save')" :loading="nicknameForm.busy.value" class="sm:mb-6" />
          </form>
          <Message v-if="nicknameForm.formError.value" severity="error" :closable="false">{{
            nicknameForm.formError.value
          }}</Message>
          <div v-if="can.leave" class="flex justify-end">
            <Button
              :label="t('households.settings.leave')"
              icon="pi pi-sign-out"
              severity="danger"
              outlined
              @click="leave"
            />
          </div>
          <p v-else class="text-sm text-muted-color">{{ t('households.settings.ownerCannotLeave') }}</p>
        </div>
      </template>
    </Card>

    <Card v-if="can.edit">
      <template #title>{{ t('households.settings.details') }}</template>
      <template #content>
        <form class="flex flex-col gap-4" novalidate @submit.prevent="saveDetails">
          <Message v-if="detailsForm.formError.value" severity="error" :closable="false">{{
            detailsForm.formError.value
          }}</Message>
          <FormField id="householdName" :label="t('households.name')" :errors="detailsForm.fieldErrors.value.name">
            <template #default="{ invalid }">
              <InputText id="householdName" v-model="details.name" :invalid="invalid" />
            </template>
          </FormField>
          <FormField
            id="householdDescription"
            :label="t('households.description')"
            :errors="detailsForm.fieldErrors.value.description"
          >
            <template #default="{ invalid }">
              <Textarea
                id="householdDescription"
                v-model="details.description"
                rows="2"
                auto-resize
                :invalid="invalid"
              />
            </template>
          </FormField>
          <div class="flex justify-end">
            <Button type="submit" :label="t('common.save')" icon="pi pi-check" :loading="detailsForm.busy.value" />
          </div>
        </form>
      </template>
    </Card>

    <PluginsSettings v-if="can.member" :household="household" />

    <Card v-if="can.archive" class="border border-red-200 dark:border-red-900">
      <template #title>{{ t('households.settings.archive') }}</template>
      <template #content>
        <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <p class="text-muted-color">{{ t('households.settings.archiveHint') }}</p>
          <Button :label="t('households.settings.archive')" severity="danger" outlined @click="archive" />
        </div>
      </template>
    </Card>
  </div>
</template>
