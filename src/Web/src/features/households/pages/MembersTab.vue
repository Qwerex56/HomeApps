<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import type { MenuItem } from 'primevue/menuitem'
import Card from 'primevue/card'
import Avatar from 'primevue/avatar'
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import Menu from 'primevue/menu'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { useSessionStore } from '@/features/auth/session'
import {
  householdKeys,
  householdsApi,
  type Household,
  type HouseholdPermissions,
  type HouseholdRole,
  type Member,
} from '../api'
import { roleSeverity } from '../tags'

// M1–M7. Inactive accounts stay on the list, greyed out (D6).
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const session = useSessionStore()

const { data: members, isPending } = useQuery({
  queryKey: computed(() => householdKeys.members(props.household.id)),
  queryFn: () => householdsApi.members(props.household.id),
})
const order: Record<HouseholdRole, number> = { Owner: 0, Admin: 1, Member: 2, Guest: 3 }
const sorted = computed(() =>
  [...(members.value ?? [])].sort((a, b) => order[a.role] - order[b.role] || a.name.localeCompare(b.name, 'pl')),
)

async function refresh(message: string) {
  toast.add({ severity: 'success', summary: message, life: 3000 })
  await queryClient.invalidateQueries({ queryKey: householdKeys.all })
}

async function run(action: () => Promise<void>, message: string) {
  try {
    await action()
    await refresh(message)
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}

const id = computed(() => props.household.id)
const isMe = (member: Member) => member.userId === session.claims?.sub

// The menu of one member, with only what my role allows.
const menu = ref<InstanceType<typeof Menu>>()
const menuItems = ref<MenuItem[]>([])
function actions(member: Member): MenuItem[] {
  if (isMe(member)) return []
  const items: MenuItem[] = []
  if (props.can.changeRoles) {
    for (const role of ['Admin', 'Member', 'Guest'] as const) {
      if (role !== member.role) {
        items.push({
          label: t('households.members.makeRole', { role: t(`households.role.${role}`) }),
          icon: 'pi pi-id-card',
          command: () =>
            run(() => householdsApi.setRole(id.value, member.userId, role), t('households.members.roleChanged')),
        })
      }
    }
  }
  if (props.can.manage(member.role)) {
    items.push({ label: t('households.members.rename'), icon: 'pi pi-pencil', command: () => openNickname(member) })
    if (member.role === 'Guest') {
      items.push({ label: t('households.members.access'), icon: 'pi pi-clock', command: () => openAccess(member) })
    }
  }
  if (props.can.transfer && member.isActive && member.role !== 'Guest') {
    items.push({ label: t('households.members.transfer'), icon: 'pi pi-crown', command: () => transfer(member) })
  }
  if (props.can.manage(member.role)) {
    items.push({ separator: true })
    items.push({
      label: t('households.members.remove'),
      icon: 'pi pi-user-minus',
      class: 'text-red-500',
      command: () => remove(member),
    })
  }
  return items
}
function openMenu(event: Event, member: Member) {
  menuItems.value = actions(member)
  menu.value?.toggle(event)
}

function remove(member: Member) {
  confirm.require({
    header: t('households.members.removeTitle'),
    message: t('households.members.removeText', { name: member.name }),
    acceptProps: { label: t('households.members.remove'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => run(() => householdsApi.remove(id.value, member.userId), t('households.members.removed')),
  })
}

function transfer(member: Member) {
  confirm.require({
    header: t('households.members.transferTitle'),
    message: t('households.members.transferText', { name: member.name }),
    acceptProps: { label: t('households.members.transfer') },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => run(() => householdsApi.transfer(id.value, member.userId), t('households.members.transferred')),
  })
}

// Someone's nickname in this household (e.g. a guest), M3.
const editing = ref<Member | null>(null)
const nickname = ref('')
const nicknameForm = useSubmit()
function openNickname(member: Member) {
  editing.value = member
  nickname.value = member.nickname ?? ''
}
async function saveNickname() {
  const member = editing.value!
  if (
    await nicknameForm.submit(() => householdsApi.setNickname(id.value, member.userId, nickname.value.trim() || null))
  ) {
    editing.value = null
    await refresh(t('households.members.renamed'))
  }
}

// When a guest's access ends, M6.
const accessFor = ref<Member | null>(null)
const until = ref<Date | null>(null)
const accessForm = useSubmit()
function openAccess(member: Member) {
  accessFor.value = member
  until.value = member.accessUntil ? new Date(member.accessUntil) : null
}
async function saveAccess() {
  const member = accessFor.value!
  if (
    await accessForm.submit(() =>
      householdsApi.setAccess(id.value, member.userId, until.value ? until.value.toISOString() : null),
    )
  ) {
    accessFor.value = null
    await refresh(t('households.members.accessSaved'))
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('households.tabs.members') }}</template>
    <template #content>
      <div v-if="isPending" class="flex flex-col gap-3">
        <Skeleton v-for="i in 3" :key="i" height="3rem" />
      </div>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li
          v-for="member in sorted"
          :key="member.userId"
          class="flex items-center justify-between gap-3 py-3"
          :class="{ 'opacity-50': !member.isActive }"
        >
          <div class="flex min-w-0 items-center gap-3">
            <Avatar v-if="member.avatarUrl" :image="member.avatarUrl" shape="circle" />
            <Avatar v-else :label="member.name.slice(0, 1).toUpperCase()" shape="circle" />
            <div class="min-w-0">
              <div class="truncate font-medium">
                {{ member.name }}
                <span v-if="isMe(member)" class="text-sm font-normal text-muted-color">{{
                  t('households.members.you')
                }}</span>
              </div>
              <div class="truncate text-sm text-muted-color">
                <span v-if="member.nickname">{{ member.displayName }} · </span>
                <span v-if="!member.isActive">{{ t('households.members.inactive') }} · </span>
                <span v-if="member.accessUntil">{{
                  t('households.members.until', { date: d(new Date(member.accessUntil), 'short') })
                }}</span>
                <span v-else>{{
                  t('households.members.joined', { date: d(new Date(member.joinedAt), 'short') })
                }}</span>
              </div>
            </div>
          </div>
          <div class="flex shrink-0 items-center gap-2">
            <Tag :value="t(`households.role.${member.role}`)" :severity="roleSeverity[member.role]" />
            <Button
              v-if="actions(member).length"
              icon="pi pi-ellipsis-v"
              severity="secondary"
              text
              rounded
              :aria-label="t('households.members.actions', { name: member.name })"
              aria-haspopup="true"
              @click="openMenu($event, member)"
            />
          </div>
        </li>
      </ul>
      <Menu ref="menu" :model="menuItems" popup />
    </template>
  </Card>

  <Dialog
    :visible="!!editing"
    modal
    :header="t('households.members.rename')"
    class="w-full max-w-sm"
    @update:visible="editing = null"
  >
    <form id="nickname-form" class="flex flex-col gap-4" @submit.prevent="saveNickname">
      <Message v-if="nicknameForm.formError.value" severity="error" :closable="false">{{
        nicknameForm.formError.value
      }}</Message>
      <FormField
        id="memberNickname"
        :label="t('households.nickname')"
        :hint="t('households.nicknameHint', { name: editing?.displayName })"
        :errors="nicknameForm.fieldErrors.value.nickname"
      >
        <InputText id="memberNickname" v-model="nickname" autofocus />
      </FormField>
    </form>
    <template #footer>
      <Button :label="t('common.cancel')" severity="secondary" text @click="editing = null" />
      <Button type="submit" form="nickname-form" :label="t('common.save')" :loading="nicknameForm.busy.value" />
    </template>
  </Dialog>

  <Dialog
    :visible="!!accessFor"
    modal
    :header="t('households.members.access')"
    class="w-full max-w-sm"
    @update:visible="accessFor = null"
  >
    <div class="flex flex-col gap-4">
      <Message v-if="accessForm.formError.value" severity="error" :closable="false">{{
        accessForm.formError.value
      }}</Message>
      <FormField
        id="accessUntil"
        :label="t('households.members.accessUntil')"
        :hint="t('households.members.accessHint')"
      >
        <DatePicker
          v-model="until"
          input-id="accessUntil"
          show-time
          hour-format="24"
          :min-date="new Date()"
          show-button-bar
          fluid
        />
      </FormField>
    </div>
    <template #footer>
      <Button :label="t('common.cancel')" severity="secondary" text @click="accessFor = null" />
      <Button :label="t('common.save')" :loading="accessForm.busy.value" @click="saveAccess" />
    </template>
  </Dialog>
</template>
