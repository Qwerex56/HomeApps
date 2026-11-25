<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { adminApi, adminKeys } from '../api'

const { t } = useI18n()
const {
  data: about,
  isPending,
  isError,
  isFetching,
  refetch,
} = useQuery({
  queryKey: adminKeys.about,
  queryFn: () => adminApi.about(),
  staleTime: 60_000,
})
</script>

<template>
  <Card>
    <template #title>
      <div class="flex items-center justify-between gap-2">
        <span>{{ t('admin.about.title') }}</span>
        <Button
          icon="pi pi-refresh"
          text
          rounded
          :loading="isFetching"
          :aria-label="t('admin.about.refresh')"
          @click="refetch()"
        />
      </div>
    </template>
    <template #content>
      <Skeleton v-if="isPending" height="8rem" />
      <Message v-else-if="isError || !about" severity="error" :closable="false">{{ t('errors.unknown') }}</Message>
      <div v-else class="flex flex-col gap-6">
        <div class="flex flex-wrap items-center gap-3">
          <span class="font-medium">{{ t('admin.about.current', { version: about.currentVersion }) }}</span>
          <Tag v-if="about.isUpdateAvailable === true" severity="warn" :value="t('admin.about.updateAvailable')" />
          <Tag v-else-if="about.isUpdateAvailable === false" severity="success" :value="t('admin.about.upToDate')" />
          <Tag v-else severity="secondary" :value="t('admin.about.updateUnknown')" />
          <span v-if="about.latestVersion" class="text-sm text-muted-color">
            {{ t('admin.about.latest', { version: about.latestVersion }) }}
          </span>
          <a
            v-if="about.releaseUrl"
            :href="about.releaseUrl"
            target="_blank"
            rel="noopener"
            class="text-sm text-primary"
          >
            {{ t('admin.about.release') }}
          </a>
        </div>

        <section>
          <h3 class="mb-2 font-medium">{{ t('admin.about.services') }}</h3>
          <ul class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
            <li v-for="service in about.services" :key="service.name" class="flex flex-wrap items-center gap-3 py-2">
              <span class="w-32 font-medium">{{ service.name }}</span>
              <span class="w-20 text-sm">{{ service.version ?? t('admin.about.noAnswer') }}</span>
              <span class="text-sm text-muted-color">
                {{
                  t('admin.about.migrations', { count: service.appliedMigrations, last: service.lastMigration ?? '-' })
                }}
              </span>
            </li>
          </ul>
        </section>

        <section>
          <h3 class="mb-2 font-medium">{{ t('admin.about.plugins') }}</h3>
          <p v-if="!about.plugins.length" class="text-sm text-muted-color">{{ t('admin.about.noPlugins') }}</p>
          <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
            <li v-for="plugin in about.plugins" :key="plugin.id" class="flex flex-wrap items-center gap-3 py-2">
              <span class="w-32 font-medium">{{ plugin.id }}</span>
              <span class="w-20 text-sm">{{ plugin.version ?? '-' }}</span>
              <Tag
                :severity="plugin.status === 'healthy' ? 'success' : 'danger'"
                :value="t(`admin.about.${plugin.status}`)"
              />
            </li>
          </ul>
        </section>
      </div>
    </template>
  </Card>
</template>
