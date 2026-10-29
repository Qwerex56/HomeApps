<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInfiniteQuery, useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import Dialog from 'primevue/dialog'
import Textarea from 'primevue/textarea'
import InputText from 'primevue/inputtext'
import Skeleton from 'primevue/skeleton'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { useSessionStore } from '@/features/auth/session'
import { describeUserAgent } from '@/features/account/securityApi'
import ProfileForm from '@/features/account/components/ProfileForm.vue'
import type { ProfileInput } from '@/features/account/api'
import { HISTORY_PAGE, adminApi, adminKeys, permissions, type HistoryEntry, type ServerRole } from '../api'
import { roleSeverity, statusSeverity } from '../tags'

// UC-11 to UC-17, A-22, A-23: one account for an admin.
const props = defineProps<{ id: string }>()
const { t, te, d } = useI18n()
const router = useRouter()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const session = useSessionStore()

const { data: user, error } = useQuery({
  queryKey: computed(() => adminKeys.user(props.id)),
  queryFn: () => adminApi.get(props.id),
})
const history = useInfiniteQuery({
  queryKey: computed(() => adminKeys.history(props.id)),
  queryFn: ({ pageParam }) => adminApi.history(props.id, pageParam),
  initialPageParam: undefined as string | undefined,
  getNextPageParam: (last) => (last.length === HISTORY_PAGE ? last[last.length - 1]!.at : undefined),
})
const entries = computed(() => history.data.value?.pages.flat() ?? [])

// Accounts stores the details as names from the code: the source, the new role or the changed profile fields.
function details(entry: HistoryEntry) {
  if (!entry.details) return null
  if (entry.action === 'Registered') return t(`admin.source.${entry.details}`)
  if (entry.action === 'RoleSynced') return t(`admin.role.${entry.details}`)
  if (entry.action === 'ProfileUpdated') {
    return entry.details
      .split(', ')
      .map((field) => (te(`admin.fields.${field}`) ? t(`admin.fields.${field}`) : field))
      .join(', ')
  }
  return entry.details
}

const can = computed(() =>
  user.value && session.claims ? permissions({ id: session.claims.sub, role: session.claims.role }, user.value) : null,
)

async function refresh(message: string) {
  toast.add({ severity: 'success', summary: message, life: 4000 })
  await queryClient.invalidateQueries({ queryKey: adminKeys.users })
}

async function run(action: () => Promise<void>, message: string) {
  try {
    await action()
    await refresh(message)
  } catch (problem) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(problem)), life: 6000 })
  }
}

const saveProfile = (input: ProfileInput) => adminApi.updateProfile(props.id, input)

// A-23: Auth changes the role; Accounts gets it from Auth's event, so its copy is read again a moment later.
const roles = computed(() =>
  (['User', 'Admin'] as const).map((role) => ({ value: role, label: t(`admin.role.${role}`) })),
)
async function changeRole(role: Exclude<ServerRole, 'Owner'>) {
  await run(() => adminApi.changeRole(props.id, role), t('admin.user.roleChanged'))
  setTimeout(() => queryClient.invalidateQueries({ queryKey: adminKeys.users }), 1500)
}

function resetPassword() {
  confirm.require({
    header: t('admin.user.resetTitle'),
    message: t('admin.user.resetText', { email: user.value?.email }),
    acceptProps: { label: t('admin.user.reset') },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => run(() => adminApi.resetPassword(props.id), t('admin.user.resetDone')),
  })
}

const enable = () => run(() => adminApi.enable(props.id), t('admin.user.enabled'))

// UC-14: disabling ends every session (Auth hears it from Accounts).
const disableOpen = ref(false)
const reason = ref('')
const disabling = useSubmit()
async function disable() {
  if (await disabling.submit(() => adminApi.disable(props.id, reason.value.trim() || null))) {
    disableOpen.value = false
    reason.value = ''
    await refresh(t('admin.user.disabled'))
  }
}

// UC-17: "forget me". Typing the email is the confirmation the service checks (Q1).
const eraseOpen = ref(false)
const confirmEmail = ref('')
const erasing = useSubmit()
async function erase() {
  if (await erasing.submit(() => adminApi.erase(props.id, confirmEmail.value.trim()))) {
    eraseOpen.value = false
    queryClient.removeQueries({ queryKey: adminKeys.user(props.id) })
    await refresh(t('admin.user.erased'))
    await router.replace({ name: 'admin-users' })
  }
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <RouterLink :to="{ name: 'admin-users' }" class="flex items-center gap-2 text-sm text-muted-color hover:underline">
      <i class="pi pi-arrow-left" aria-hidden="true" />{{ t('admin.users.title') }}
    </RouterLink>

    <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
    <Skeleton v-else-if="!user" height="10rem" />
    <template v-else>
      <Card>
        <template #content>
          <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div class="min-w-0">
              <h1 class="truncate text-2xl font-semibold">{{ user.profile.displayName }}</h1>
              <div class="truncate text-muted-color">{{ user.email }}</div>
              <div class="mt-2 flex gap-2">
                <Tag :value="t(`admin.role.${user.role}`)" :severity="roleSeverity[user.role]" />
                <Tag :value="t(`admin.status.${user.status}`)" :severity="statusSeverity[user.status]" />
              </div>
            </div>
            <div class="flex flex-wrap gap-2">
              <Select
                v-if="can?.changeRole"
                :model-value="user.role"
                :options="roles"
                option-label="label"
                option-value="value"
                :aria-label="t('admin.users.role')"
                @update:model-value="changeRole"
              />
              <Button
                v-if="can?.resetPassword"
                :label="t('admin.user.reset')"
                icon="pi pi-key"
                severity="secondary"
                outlined
                @click="resetPassword"
              />
              <Button
                v-if="can?.enable"
                :label="t('admin.user.enable')"
                icon="pi pi-check-circle"
                severity="success"
                outlined
                @click="enable"
              />
              <Button
                v-if="can?.disable"
                :label="t('admin.user.disable')"
                icon="pi pi-ban"
                severity="danger"
                outlined
                @click="disableOpen = true"
              />
              <Button
                v-if="can?.erase"
                :label="t('admin.user.erase')"
                icon="pi pi-trash"
                severity="danger"
                @click="eraseOpen = true"
              />
            </div>
          </div>

          <Message v-if="user.disabling" severity="warn" :closable="false" class="mt-4">
            {{
              user.disabling.bySelf
                ? t('admin.user.disabledBySelf', { date: d(new Date(user.disabling.at), 'long') })
                : t('admin.user.disabledByAdmin', { date: d(new Date(user.disabling.at), 'long') })
            }}
            <span v-if="user.disabling.reason">{{ t('admin.user.reason', { reason: user.disabling.reason }) }}</span>
          </Message>

          <dl class="mt-4 grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2 lg:grid-cols-3">
            <div>
              <dt class="text-muted-color">{{ t('admin.user.registered') }}</dt>
              <dd>
                {{ d(new Date(user.registration.registeredAt), 'long') }} ·
                {{ t(`admin.source.${user.registration.source}`) }}
              </dd>
            </div>
            <div>
              <dt class="text-muted-color">{{ t('admin.users.lastSeen') }}</dt>
              <dd>{{ user.lastSeenAt ? d(new Date(user.lastSeenAt), 'long') : t('admin.users.never') }}</dd>
            </div>
            <div v-if="user.registration.viaReferral">
              <dt class="text-muted-color">{{ t('admin.user.invitedBy') }}</dt>
              <dd>
                {{ user.registration.invitedByEmail ?? '—' }}
                <span v-if="user.registration.invitedEmail && user.registration.invitedEmail !== user.email">
                  ({{ t('admin.user.invitedAs', { email: user.registration.invitedEmail }) }})
                </span>
              </dd>
            </div>
            <div v-if="user.registration.userAgent">
              <dt class="text-muted-color">{{ t('admin.user.device') }}</dt>
              <dd>{{ describeUserAgent(user.registration.userAgent) ?? user.registration.userAgent }}</dd>
            </div>
          </dl>
        </template>
      </Card>

      <Card>
        <template #title>{{ t('account.profile.title') }}</template>
        <template #content>
          <ProfileForm
            v-if="can?.manage"
            :profile="user.profile"
            :save="saveProfile"
            @saved="refresh(t('account.profile.saved'))"
          />
          <p v-else class="text-muted-color">{{ t('admin.user.readOnly') }}</p>
        </template>
      </Card>
    </template>

    <Card>
      <template #title>{{ t('account.history.title') }}</template>
      <template #content>
        <Skeleton v-if="history.isPending.value" height="6rem" />
        <ol v-else class="relative ml-2 border-l border-surface-200 dark:border-surface-700">
          <li v-for="entry in entries" :key="entry.at + entry.action" class="mb-4 ml-5">
            <span class="absolute -left-1.5 mt-1.5 size-3 rounded-full bg-primary" aria-hidden="true" />
            <div>
              {{ t(`account.history.action.${entry.action}`) }}
              <span v-if="entry.details" class="text-muted-color">· {{ details(entry) }}</span>
            </div>
            <div class="text-sm text-muted-color">
              <time :datetime="entry.at">{{ d(new Date(entry.at), 'long') }}</time>
              · {{ entry.actorName ?? (entry.actorId ? t('admin.user.erasedActor') : t('admin.user.system')) }}
            </div>
            <div v-if="entry.reason" class="text-sm">{{ t('admin.user.reason', { reason: entry.reason }) }}</div>
          </li>
        </ol>
        <Button
          v-if="history.hasNextPage.value"
          :label="t('admin.user.older')"
          severity="secondary"
          text
          :loading="history.isFetchingNextPage.value"
          @click="history.fetchNextPage()"
        />
      </template>
    </Card>

    <Dialog v-model:visible="disableOpen" modal :header="t('admin.user.disableTitle')" class="w-full max-w-md">
      <div class="flex flex-col gap-4">
        <p>{{ t('admin.user.disableText', { name: user?.profile.displayName }) }}</p>
        <Message v-if="disabling.formError.value" severity="error" :closable="false">{{
          disabling.formError.value
        }}</Message>
        <FormField id="reason" :label="t('account.disable.reason')" :errors="disabling.fieldErrors.value.reason">
          <Textarea id="reason" v-model="reason" rows="3" auto-resize />
        </FormField>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" severity="secondary" text @click="disableOpen = false" />
        <Button :label="t('admin.user.disable')" severity="danger" :loading="disabling.busy.value" @click="disable" />
      </template>
    </Dialog>

    <Dialog v-model:visible="eraseOpen" modal :header="t('admin.user.eraseTitle')" class="w-full max-w-md">
      <div class="flex flex-col gap-4">
        <p>{{ t('admin.user.eraseText') }}</p>
        <Message v-if="erasing.formError.value" severity="error" :closable="false">{{
          erasing.formError.value
        }}</Message>
        <FormField
          id="confirmEmail"
          :label="t('admin.user.eraseConfirm', { email: user?.email })"
          :errors="erasing.fieldErrors.value.confirmEmail"
        >
          <template #default="{ invalid }">
            <InputText id="confirmEmail" v-model="confirmEmail" autocomplete="off" :invalid="invalid" />
          </template>
        </FormField>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" severity="secondary" text @click="eraseOpen = false" />
        <Button
          :label="t('admin.user.erase')"
          severity="danger"
          :disabled="confirmEmail.trim().toLowerCase() !== user?.email.toLowerCase()"
          :loading="erasing.busy.value"
          @click="erase"
        />
      </template>
    </Dialog>
  </div>
</template>
