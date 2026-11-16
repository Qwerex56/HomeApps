<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRouter } from 'vue-router'
import { useQuery } from '@tanstack/vue-query'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { useSessionStore } from '@/features/auth/session'
import { localized } from '@/features/plugins/registry'
import SafeMarkdown from '@/shared/components/SafeMarkdown.vue'
import { homemixApi, mixKeys } from '../api'
import HeartButton from './HeartButton.vue'
import InstallFromStore from './InstallFromStore.vue'
import { useListingActions } from './useListingActions'

// An add-on's page in the store (homemix-2.md 2.2): what it is, its numbers, what it asks for, its README (Markdown
// without HTML or outside images, S5) and "Zainstaluj". Its author (or the Owner) refreshes or removes it here.
const props = defineProps<{ listingKey: string }>()
const { t, d } = useI18n()
const router = useRouter()
const session = useSessionStore()
const entry = useQuery({
  queryKey: computed(() => mixKeys.storeEntry(props.listingKey)),
  queryFn: () => homemixApi.store.entry(props.listingKey),
})
const { busy, refresh, remove } = useListingActions()
const e = computed(() => entry.data.value)
const canChange = computed(
  () => !!e.value && !e.value.builtin && !e.value.official && (e.value.mine || session.isOwner),
)
</script>

<template>
  <div class="flex flex-col gap-6">
    <RouterLink :to="{ name: 'store' }" class="flex items-center gap-2 text-sm text-muted-color hover:underline">
      <i class="pi pi-arrow-left" aria-hidden="true" />{{ t('homemix.store.back') }}
    </RouterLink>
    <Message v-if="entry.error.value" severity="error" :closable="false">
      {{ errorMessage(toProblem(entry.error.value)) }}
    </Message>
    <Skeleton v-else-if="!e" height="12rem" />
    <template v-else>
      <div class="flex flex-wrap items-start gap-4">
        <i :class="['pi', e.icon, 'text-4xl text-primary']" aria-hidden="true" />
        <div class="min-w-0 flex-1">
          <div class="flex flex-wrap items-center gap-2">
            <h1 class="text-2xl font-semibold">{{ localized(e.name) }}</h1>
            <Tag v-if="e.official" :value="t('homemix.store.official')" severity="success" />
            <Tag v-if="e.mine" :value="t('homemix.store.mine')" severity="info" />
            <Tag v-if="e.status === 'Hidden'" :value="t('homemix.store.hidden')" severity="warn" />
          </div>
          <p v-if="e.description" class="text-muted-color">{{ localized(e.description) }}</p>
          <div class="mt-1 flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-color">
            <span>{{ t('homemix.store.version', { version: e.version }) }}</span>
            <span v-if="e.publishedAt">{{ t('homemix.store.published', { date: d(e.publishedAt, 'short') }) }}</span>
            <span class="flex items-center gap-1">
              <i class="pi pi-download" aria-hidden="true" />{{ t('homemix.store.installs', Number(e.installs)) }}
              <template v-if="Number(e.installs30)"
                >({{ t('homemix.store.installs30', { n: e.installs30 }) }})</template
              >
            </span>
            <a
              v-if="e.repository"
              :href="e.repository"
              target="_blank"
              rel="noopener noreferrer"
              class="flex items-center gap-1 text-primary hover:underline"
            >
              <i class="pi pi-github" aria-hidden="true" />{{ t('homemix.store.repository') }}
            </a>
          </div>
        </div>
        <HeartButton :entry="e" />
      </div>

      <Message v-if="e.problem" severity="warn" :closable="false">{{
        t(`homemix.store.problem.${e.problem}`)
      }}</Message>
      <InstallFromStore :entry="e" />

      <div class="grid gap-6 lg:grid-cols-[1fr_18rem]">
        <Card>
          <template #content>
            <SafeMarkdown v-if="e.readme" :source="e.readme" />
            <p v-else class="text-muted-color">{{ t('homemix.store.noReadme') }}</p>
          </template>
        </Card>
        <aside class="flex flex-col gap-4">
          <Card>
            <template #title>{{ t('homemix.store.asks') }}</template>
            <template #content>
              <ul class="list-inside list-disc text-sm">
                <li>{{ t('homemix.consent.always') }}</li>
                <li v-for="c in e.capabilities" :key="c">{{ t(`homemix.capability["${c}"]`) }}</li>
                <li v-if="e.network.length">{{ t('homemix.store.network', { hosts: e.network.join(', ') }) }}</li>
                <li v-else>{{ t('homemix.store.noNetwork') }}</li>
                <li v-if="e.jarvis">{{ t('homemix.store.jarvis') }}</li>
              </ul>
            </template>
          </Card>
          <Card>
            <template #title>{{ t('homemix.store.targets') }}</template>
            <template #content>
              <div class="flex gap-2">
                <Tag
                  v-for="target in e.targets"
                  :key="target"
                  :value="t(`homemix.store.filter.${target === 'Household' ? 'household' : 'user'}`)"
                  severity="secondary"
                />
              </div>
            </template>
          </Card>
          <div v-if="canChange" class="flex flex-wrap gap-2">
            <Button
              :label="t('homemix.store.refresh')"
              icon="pi pi-refresh"
              size="small"
              severity="secondary"
              :loading="busy === e.listingKey"
              @click="refresh(e.listingKey)"
            />
            <Button
              :label="t('homemix.store.remove')"
              icon="pi pi-trash"
              size="small"
              severity="danger"
              text
              :disabled="busy === e.listingKey"
              @click="remove(e.listingKey, localized(e.name), () => router.push({ name: 'store' }))"
            />
          </div>
        </aside>
      </div>
    </template>
  </div>
</template>
