<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import { useQuery } from '@tanstack/vue-query'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys } from '../api'
import DailyChart from './DailyChart.vue'
import { useListingActions } from './useListingActions'

// "Konto" (homemix-2.md 2.2): each of my entries with its numbers, never who installed it.
const { t } = useI18n()
const mine = useQuery({ queryKey: mixKeys.storeMine, queryFn: homemixApi.store.mine })
const { busy, refresh, remove } = useListingActions()
const numbers = ['active', 'households', 'accounts', 'installs', 'updates', 'removals'] as const
</script>

<template>
  <div class="flex flex-col gap-4">
    <div>
      <h2 class="text-lg font-semibold">{{ t('homemix.store.account.title') }}</h2>
      <p class="text-sm text-muted-color">{{ t('homemix.store.account.subtitle') }}</p>
    </div>
    <Message v-if="mine.error.value" severity="error" :closable="false">
      {{ errorMessage(toProblem(mine.error.value)) }}
    </Message>
    <Skeleton v-else-if="mine.isPending.value" height="12rem" />
    <Message v-else-if="!mine.data.value?.length" severity="secondary" :closable="false">
      {{ t('homemix.store.account.empty') }}
    </Message>
    <Card v-for="{ entry, counts, versions, days } in mine.data.value" v-else :key="entry.listingKey">
      <template #title>
        <div class="flex flex-wrap items-center gap-2">
          <i :class="['pi', entry.icon, 'text-primary']" aria-hidden="true" />
          <RouterLink :to="{ name: 'store-entry', params: { listingKey: entry.listingKey } }" class="hover:underline">
            {{ localized(entry.name) }}
          </RouterLink>
          <span class="text-sm font-normal text-muted-color">{{ entry.version }}</span>
          <Tag v-if="entry.status === 'Hidden'" :value="t('homemix.store.hidden')" severity="warn" />
          <span class="ml-auto flex items-center gap-1 text-sm font-normal text-muted-color">
            <i class="pi pi-home" aria-hidden="true" />{{ t('homemix.store.hearts', Number(entry.hearts)) }}
          </span>
        </div>
      </template>
      <template #content>
        <div class="flex flex-col gap-4">
          <Message v-if="entry.problem" severity="warn" :closable="false">
            {{ t(`homemix.store.problem.${entry.problem}`) }}
          </Message>
          <dl class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
            <div v-for="n in numbers" :key="n" class="rounded-md bg-emphasis p-2">
              <dt class="text-xs text-muted-color">{{ t(`homemix.store.account.${n}`) }}</dt>
              <dd class="text-xl font-semibold">{{ counts[n] }}</dd>
            </div>
          </dl>
          <DailyChart :days="days" />
          <div>
            <h3 class="text-sm text-muted-color">{{ t('homemix.store.account.versions') }}</h3>
            <p v-if="!versions.length" class="text-sm">{{ t('homemix.store.account.noVersions') }}</p>
            <ul v-else class="flex flex-wrap gap-2">
              <li v-for="v in versions" :key="v.version">
                <Tag :value="`${v.version}: ${v.installations}`" severity="secondary" />
              </li>
            </ul>
          </div>
          <div class="flex flex-wrap gap-2">
            <Button
              :label="t('homemix.store.refresh')"
              icon="pi pi-refresh"
              size="small"
              severity="secondary"
              :loading="busy === entry.listingKey"
              @click="refresh(entry.listingKey)"
            />
            <Button
              :label="t('homemix.store.remove')"
              icon="pi pi-trash"
              size="small"
              severity="danger"
              text
              :disabled="busy === entry.listingKey"
              @click="remove(entry.listingKey, localized(entry.name))"
            />
          </div>
        </div>
      </template>
    </Card>
  </div>
</template>
