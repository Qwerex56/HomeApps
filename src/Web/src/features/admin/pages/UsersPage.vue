<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter, type LocationQuery } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { keepPreviousData, useQuery } from '@tanstack/vue-query'
import Card from 'primevue/card'
import DataTable, { type DataTablePageEvent, type DataTableSortEvent } from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { adminApi, adminKeys, type AccountStatus, type ServerRole, type UserFilter, type UserSort } from '../api'
import { roleSeverity, statusSeverity } from '../tags'

// UC-10. The filter lives in the URL, so going back from an account returns to the same page of the list.
const { t, d } = useI18n()
const route = useRoute()
const router = useRouter()

const sorts: UserSort[] = ['DisplayName', 'Email', 'Role', 'Status', 'CreatedAt', 'LastSeenAt']
const one = (value: LocationQuery[string]) => (Array.isArray(value) ? value[0] : value) ?? undefined

const filter = computed<UserFilter>(() => {
  const q = route.query
  const sort = one(q.sort) as UserSort | undefined
  return {
    status: (one(q.status) as AccountStatus | undefined) ?? null,
    role: (one(q.role) as ServerRole | undefined) ?? null,
    q: one(q.q) ?? '',
    page: Math.max(1, Number(one(q.page)) || 1),
    pageSize: 20,
    sort: sort && sorts.includes(sort) ? sort : 'DisplayName',
    desc: one(q.desc) === 'true',
  }
})

function update(change: Partial<UserFilter>) {
  const next = { ...filter.value, page: 1, ...change }
  const query: Record<string, string> = {}
  if (next.status) query.status = next.status
  if (next.role) query.role = next.role
  if (next.q) query.q = next.q
  if (next.page > 1) query.page = String(next.page)
  if (next.sort !== 'DisplayName') query.sort = next.sort
  if (next.desc) query.desc = 'true'
  router.replace({ query })
}

const { data, isFetching, error } = useQuery({
  queryKey: computed(() => adminKeys.list(filter.value)),
  queryFn: () => adminApi.list(filter.value),
  placeholderData: keepPreviousData,
})

// Typing waits a moment before it asks Accounts (ILIKE on name and email, Q4).
const search = ref(filter.value.q ?? '')
let timer: ReturnType<typeof setTimeout> | undefined
watch(search, (value) => {
  clearTimeout(timer)
  timer = setTimeout(() => update({ q: value.trim() }), 300)
})

const statusOptions = computed(() => [
  { value: 'Active', label: t('admin.status.Active') },
  { value: 'Disabled', label: t('admin.status.Disabled') },
])
const roleOptions = computed(() => [
  ...(['User', 'Admin', 'Owner'] as const).map((role) => ({ value: role, label: t(`admin.role.${role}`) })),
])

function onPage(event: DataTablePageEvent) {
  update({ page: event.page + 1, sort: filter.value.sort, desc: filter.value.desc })
}

function onSort(event: DataTableSortEvent) {
  update({ sort: event.sortField as UserSort, desc: event.sortOrder === -1 })
}
</script>

<template>
  <Card>
    <template #title>{{ t('admin.users.title') }}</template>
    <template #content>
      <div class="mb-4 flex flex-col gap-3 sm:flex-row">
        <IconField class="flex-1">
          <InputIcon class="pi pi-search" />
          <InputText
            v-model="search"
            :placeholder="t('admin.users.search')"
            :aria-label="t('admin.users.search')"
            fluid
          />
        </IconField>
        <Select
          :model-value="filter.status"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          :aria-label="t('admin.users.status')"
          :placeholder="t('admin.users.anyStatus')"
          show-clear
          class="sm:w-44"
          @update:model-value="update({ status: $event ?? null })"
        />
        <Select
          :model-value="filter.role"
          :options="roleOptions"
          option-label="label"
          option-value="value"
          :aria-label="t('admin.users.role')"
          :placeholder="t('admin.users.anyRole')"
          show-clear
          class="sm:w-44"
          @update:model-value="update({ role: $event ?? null })"
        />
      </div>
      <Message v-if="error" severity="error" :closable="false" class="mb-4">{{
        errorMessage(toProblem(error))
      }}</Message>
      <DataTable
        :value="data?.items ?? []"
        data-key="id"
        lazy
        paginator
        :rows="filter.pageSize"
        :first="(filter.page - 1) * filter.pageSize"
        :total-records="data?.total ?? 0"
        :loading="isFetching"
        :sort-field="filter.sort"
        :sort-order="filter.desc ? -1 : 1"
        row-hover
        selection-mode="single"
        @page="onPage"
        @sort="onSort"
        @row-select="router.push({ name: 'admin-user', params: { id: $event.data.id } })"
      >
        <template #empty>{{ t('admin.users.empty') }}</template>
        <Column field="displayName" sort-field="DisplayName" :header="t('auth.fields.displayName')" sortable>
          <template #body="{ data: user }">
            <RouterLink :to="{ name: 'admin-user', params: { id: user.id } }" class="font-medium hover:underline">
              {{ user.displayName }}
            </RouterLink>
          </template>
        </Column>
        <Column field="email" sort-field="Email" :header="t('auth.fields.email')" sortable />
        <Column field="role" sort-field="Role" :header="t('admin.users.role')" sortable>
          <template #body="{ data: user }">
            <Tag :value="t(`admin.role.${user.role}`)" :severity="roleSeverity[user.role as ServerRole]" />
          </template>
        </Column>
        <Column field="status" sort-field="Status" :header="t('admin.users.status')" sortable>
          <template #body="{ data: user }">
            <Tag :value="t(`admin.status.${user.status}`)" :severity="statusSeverity[user.status as AccountStatus]" />
          </template>
        </Column>
        <Column field="lastSeenAt" sort-field="LastSeenAt" :header="t('admin.users.lastSeen')" sortable>
          <template #body="{ data: user }">
            <span class="text-sm text-muted-color">{{
              user.lastSeenAt ? d(new Date(user.lastSeenAt), 'short') : t('admin.users.never')
            }}</span>
          </template>
        </Column>
      </DataTable>
    </template>
  </Card>
</template>
