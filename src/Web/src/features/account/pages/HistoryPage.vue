<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Skeleton from 'primevue/skeleton'
import { accountApi, accountKeys } from '../api'

// UC-04 (Q6): only when and what kind of change; who did it is admin data.
const { t, d } = useI18n()
const { data: entries, isPending } = useQuery({ queryKey: accountKeys.history, queryFn: accountApi.history })
</script>

<template>
  <Card>
    <template #title>{{ t('account.history.title') }}</template>
    <template #subtitle>{{ t('account.history.subtitle') }}</template>
    <template #content>
      <div v-if="isPending" class="flex flex-col gap-3">
        <Skeleton v-for="i in 4" :key="i" height="2rem" />
      </div>
      <ol v-else class="relative ml-2 border-l border-surface-200 dark:border-surface-700">
        <li v-for="entry in entries" :key="entry.at + entry.action" class="mb-4 ml-5">
          <span class="absolute -left-1.5 mt-1.5 size-3 rounded-full bg-primary" aria-hidden="true" />
          <div>{{ t(`account.history.action.${entry.action}`) }}</div>
          <time class="text-sm text-muted-color" :datetime="entry.at">{{ d(new Date(entry.at), 'long') }}</time>
        </li>
      </ol>
    </template>
  </Card>
</template>
