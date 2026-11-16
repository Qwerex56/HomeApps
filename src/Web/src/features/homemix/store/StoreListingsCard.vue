<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys, type StoreEntry } from '../api'

// The Owner's list of store entries (homemix-2.md 2.2), listed and hidden, to hide one or show it again.
const { t } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()
const listings = useQuery({ queryKey: mixKeys.adminStore, queryFn: homemixApi.admin.store })
const busy = ref<string | null>(null)

async function toggle(entry: StoreEntry) {
  const hide = entry.status !== 'Hidden'
  busy.value = entry.listingKey
  try {
    await homemixApi.admin.hide(entry.listingKey, hide)
    const name = localized(entry.name)
    toast.add({
      severity: 'success',
      summary: hide ? t('homemix.store.admin.hiddenDone', { name }) : t('homemix.store.admin.shownDone', { name }),
      life: 3000,
    })
    await queryClient.invalidateQueries({ queryKey: mixKeys.all })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = null
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('homemix.store.admin.title') }}</template>
    <template #subtitle>{{ t('homemix.store.admin.subtitle') }}</template>
    <template #content>
      <Skeleton v-if="listings.isPending.value" height="6rem" />
      <Message v-else-if="listings.error.value" severity="error" :closable="false">
        {{ errorMessage(toProblem(listings.error.value)) }}
      </Message>
      <Message v-else-if="!listings.data.value?.length" severity="secondary" :closable="false">
        {{ t('homemix.store.admin.empty') }}
      </Message>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li v-for="entry in listings.data.value" :key="entry.listingKey" class="flex flex-wrap items-center gap-3 py-2">
          <i :class="['pi', entry.icon, 'text-primary']" aria-hidden="true" />
          <div class="min-w-0 flex-1">
            <RouterLink
              :to="{ name: 'store-entry', params: { listingKey: entry.listingKey } }"
              class="font-medium hover:underline"
              >{{ localized(entry.name) }}</RouterLink
            >
            <span class="ml-2 text-sm text-muted-color">{{ entry.listingKey }} · {{ entry.version }}</span>
            <div v-if="entry.repository" class="text-xs break-all text-muted-color">{{ entry.repository }}</div>
          </div>
          <Tag v-if="entry.official" :value="t('homemix.store.official')" severity="success" />
          <Tag v-if="entry.status === 'Hidden'" :value="t('homemix.store.hidden')" severity="warn" />
          <span class="text-sm text-muted-color">{{ t('homemix.store.installs', Number(entry.installs)) }}</span>
          <Button
            :label="entry.status === 'Hidden' ? t('homemix.store.admin.show') : t('homemix.store.admin.hide')"
            size="small"
            severity="secondary"
            :loading="busy === entry.listingKey"
            @click="toggle(entry)"
          />
        </li>
      </ul>
    </template>
  </Card>
</template>
