<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useInfiniteQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Card from 'primevue/card'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { ADMIN_PAGE, householdKeys, householdsApi, type AdminHousehold } from '@/features/households/api'

// Q7: every household for the server Admin and Owner, newest first; restore what was archived (D7).
const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()

const search = ref('')
const q = ref('')
let timer: ReturnType<typeof setTimeout> | undefined
watch(search, (value) => {
  clearTimeout(timer)
  timer = setTimeout(() => (q.value = value.trim()), 300)
})

const list = useInfiniteQuery({
  queryKey: computed(() => householdKeys.admin(q.value)),
  queryFn: ({ pageParam }) => householdsApi.all(q.value, pageParam),
  initialPageParam: undefined as string | undefined,
  getNextPageParam: (last) => (last.length === ADMIN_PAGE ? last[last.length - 1]!.createdAt : undefined),
})
const rows = computed(() => list.data.value?.pages.flat() ?? [])

async function run(action: () => Promise<void>, message: string) {
  try {
    await action()
    toast.add({ severity: 'success', summary: message, life: 3000 })
    await queryClient.invalidateQueries({ queryKey: householdKeys.all })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}

const restore = (h: AdminHousehold) => run(() => householdsApi.restore(h.id), t('admin.households.restored'))

function archive(h: AdminHousehold) {
  confirm.require({
    header: t('households.settings.archiveTitle'),
    message: t('households.settings.archiveText', { name: h.name }),
    acceptProps: { label: t('households.settings.archive'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => run(() => householdsApi.archive(h.id), t('households.settings.archived')),
  })
}
</script>

<template>
  <Card>
    <template #title>{{ t('admin.households.title') }}</template>
    <template #content>
      <IconField class="mb-4">
        <InputIcon class="pi pi-search" />
        <InputText
          v-model="search"
          :placeholder="t('admin.households.search')"
          :aria-label="t('admin.households.search')"
          fluid
        />
      </IconField>
      <Message v-if="list.error.value" severity="error" :closable="false" class="mb-4">{{
        errorMessage(toProblem(list.error.value))
      }}</Message>
      <DataTable :value="rows" data-key="id" :loading="list.isFetching.value">
        <template #empty>{{ t('admin.households.empty') }}</template>
        <Column field="name" :header="t('households.name')">
          <template #body="{ data: h }">
            <RouterLink :to="{ name: 'household', params: { id: h.id } }" class="font-medium hover:underline">{{
              h.name
            }}</RouterLink>
          </template>
        </Column>
        <Column field="ownerName" :header="t('households.role.Owner')">
          <template #body="{ data: h }">{{ h.ownerName ?? '—' }}</template>
        </Column>
        <Column field="memberCount" :header="t('admin.households.members')" />
        <Column field="createdAt" :header="t('admin.households.created')">
          <template #body="{ data: h }">{{ d(new Date(h.createdAt), 'short') }}</template>
        </Column>
        <Column :header="t('admin.users.status')">
          <template #body="{ data: h }">
            <Tag
              :value="h.status === 'Archived' ? t('households.archived') : t('admin.status.Active')"
              :severity="h.status === 'Archived' ? 'danger' : 'success'"
            />
          </template>
        </Column>
        <Column>
          <template #body="{ data: h }">
            <Button
              v-if="h.status === 'Archived'"
              :label="t('admin.households.restore')"
              icon="pi pi-replay"
              size="small"
              severity="secondary"
              outlined
              @click="restore(h)"
            />
            <Button
              v-else
              :label="t('households.settings.archive')"
              icon="pi pi-inbox"
              size="small"
              severity="danger"
              text
              @click="archive(h)"
            />
          </template>
        </Column>
      </DataTable>
      <Button
        v-if="list.hasNextPage.value"
        :label="t('admin.user.older')"
        severity="secondary"
        text
        class="mt-2"
        :loading="list.isFetchingNextPage.value"
        @click="list.fetchNextPage()"
      />
    </template>
  </Card>
</template>
