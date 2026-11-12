<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys, type StoreEntry } from './api'

// The store (W9): official add-ons that came with HomeApps, then the GitHub repositories from the store's index. The
// README is shown as plain text: it comes from someone else's repository, so it is never rendered as HTML.
defineProps<{ busy: boolean }>()
const emit = defineEmits<{ choose: [entry: StoreEntry] }>()
const { t, d } = useI18n()

const store = useQuery({ queryKey: mixKeys.store, queryFn: homemixApi.store, staleTime: 5 * 60_000 })
const open = ref<string | null>(null)
const keyOf = (e: StoreEntry) => e.addonId ?? e.repository ?? ''
</script>

<template>
  <Skeleton v-if="store.isPending.value" height="8rem" />
  <Message v-else-if="!store.data.value?.length" severity="secondary" :closable="false">
    {{ t('homemix.store.empty') }}
  </Message>
  <ul v-else class="flex max-h-[24rem] flex-col gap-2 overflow-y-auto">
    <li
      v-for="entry in store.data.value"
      :key="keyOf(entry)"
      class="flex flex-col gap-2 rounded-md border border-surface-200 p-3 dark:border-surface-700"
    >
      <div class="flex items-start gap-3">
        <i :class="['pi', entry.icon, 'mt-1 text-xl text-primary']" aria-hidden="true" />
        <div class="min-w-0 flex-1">
          <div class="flex flex-wrap items-center gap-2 font-medium">
            {{ localized(entry.name) }}
            <Tag v-if="entry.official" :value="t('homemix.store.official')" severity="success" />
            <span v-if="entry.version" class="text-sm text-muted-color">{{ entry.version }}</span>
          </div>
          <div v-if="entry.description" class="text-sm text-muted-color">{{ localized(entry.description) }}</div>
          <a
            v-if="entry.repository"
            :href="entry.repository"
            target="_blank"
            rel="noopener noreferrer"
            class="text-sm break-all text-primary"
            >{{ entry.repository }}</a
          >
          <div v-if="entry.publishedAt" class="text-xs text-muted-color">
            {{ t('homemix.store.published', { date: d(entry.publishedAt, 'short') }) }}
          </div>
          <div v-if="entry.problem" class="text-sm text-orange-500">
            {{ t(`homemix.store.problem.${entry.problem}`) }}
          </div>
        </div>
        <Button
          :label="t('homemix.store.choose')"
          size="small"
          :disabled="busy || !!entry.problem"
          @click="emit('choose', entry)"
        />
      </div>
      <template v-if="entry.readme">
        <Button
          :label="open === keyOf(entry) ? t('homemix.store.hideReadme') : t('homemix.store.readme')"
          text
          size="small"
          class="self-start"
          @click="open = open === keyOf(entry) ? null : keyOf(entry)"
        />
        <pre
          v-if="open === keyOf(entry)"
          class="max-h-64 overflow-auto rounded bg-emphasis p-2 text-xs whitespace-pre-wrap"
          >{{ entry.readme }}</pre
        >
      </template>
    </li>
  </ul>
</template>
