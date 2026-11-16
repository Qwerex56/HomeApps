<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useInfiniteQuery } from '@tanstack/vue-query'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Skeleton from 'primevue/skeleton'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { homemixApi, mixKeys, STORE_PAGE_SIZE, type StoreQuery, type StoreTab } from '../api'
import StoreCard from './StoreCard.vue'
import StoreMine from './StoreMine.vue'
import SubmitListingDialog from './SubmitListingDialog.vue'

// The store page (homemix-2.md 2.2): tabs, search and filters, pages of 24, and "Konto" with my entries' numbers.
// The tab lives in the address (?tab=), so a link opens the same view.
const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const tabs: (StoreTab | 'mine')[] = ['all', 'featured', 'popular', 'hearts', 'new', 'mine']
const tab = computed<StoreTab | 'mine'>(() => {
  const value = route.query.tab
  return typeof value === 'string' && (tabs as string[]).includes(value) ? (value as StoreTab | 'mine') : 'all'
})
const choose = (value: StoreTab | 'mine') => router.replace({ query: value === 'all' ? {} : { tab: value } })

const search = ref('')
const q = ref('')
let timer: ReturnType<typeof setTimeout> | undefined
watch(search, (value) => {
  clearTimeout(timer)
  timer = setTimeout(() => (q.value = value), 300)
})
const target = ref<'any' | 'household' | 'user'>('any')
const jarvis = ref(false)
const offline = ref(false)
const targets = computed(() => [
  { label: t('homemix.store.filter.any'), value: 'any' },
  { label: t('homemix.store.filter.household'), value: 'household' },
  { label: t('homemix.store.filter.user'), value: 'user' },
])

const query = computed<StoreQuery>(() => ({
  tab: tab.value === 'mine' ? 'all' : tab.value,
  q: q.value,
  target: target.value === 'any' ? null : target.value,
  jarvis: jarvis.value,
  offline: offline.value,
}))
const list = useInfiniteQuery({
  queryKey: computed(() => mixKeys.storeList(query.value)),
  queryFn: ({ pageParam }) => homemixApi.store.list(query.value, pageParam),
  initialPageParam: 1,
  getNextPageParam: (last, pages) => (last.length === STORE_PAGE_SIZE ? pages.length + 1 : undefined),
  enabled: computed(() => tab.value !== 'mine'),
  staleTime: 60_000,
})
const entries = computed(() => list.data.value?.pages.flat() ?? [])
const submitting = ref(false)
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-semibold">{{ t('homemix.store.title') }}</h1>
        <p class="text-muted-color">{{ t('homemix.store.subtitle') }}</p>
      </div>
      <Button :label="t('homemix.store.submit')" icon="pi pi-github" @click="submitting = true" />
    </div>

    <nav :aria-label="t('homemix.store.title')" class="flex gap-1 overflow-x-auto">
      <button
        v-for="value in tabs"
        :key="value"
        type="button"
        class="flex items-center gap-2 rounded-md px-3 py-2 text-sm whitespace-nowrap text-muted-color hover:bg-emphasis"
        :class="{ '!text-primary bg-highlight': tab === value }"
        :aria-current="tab === value ? 'page' : undefined"
        @click="choose(value)"
      >
        {{ t(`homemix.store.tab.${value}`) }}
      </button>
    </nav>

    <StoreMine v-if="tab === 'mine'" />
    <template v-else>
      <div class="flex flex-wrap items-center gap-3">
        <IconField class="min-w-0 flex-1 basis-64">
          <InputIcon class="pi pi-search" />
          <InputText
            v-model="search"
            :placeholder="t('homemix.store.search')"
            :aria-label="t('homemix.store.search')"
            fluid
          />
        </IconField>
        <Select
          v-model="target"
          :options="targets"
          option-label="label"
          option-value="value"
          :aria-label="t('homemix.store.filter.target')"
        />
        <label class="flex items-center gap-2 text-sm">
          <Checkbox v-model="jarvis" binary input-id="store-jarvis" />{{ t('homemix.store.filter.jarvis') }}
        </label>
        <label class="flex items-center gap-2 text-sm">
          <Checkbox v-model="offline" binary input-id="store-offline" />{{ t('homemix.store.filter.offline') }}
        </label>
      </div>

      <Message v-if="list.error.value" severity="error" :closable="false">
        {{ errorMessage(toProblem(list.error.value)) }}
      </Message>
      <div v-else-if="list.isPending.value" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Skeleton v-for="n in 6" :key="n" height="10rem" />
      </div>
      <Message v-else-if="!entries.length" severity="secondary" :closable="false">
        {{ q || target !== 'any' || jarvis || offline ? t('homemix.store.nothing') : t('homemix.store.empty') }}
      </Message>
      <template v-else>
        <ul class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <li v-for="entry in entries" :key="entry.listingKey">
            <StoreCard :entry="entry" />
          </li>
        </ul>
        <Button
          v-if="list.hasNextPage.value"
          :label="t('homemix.store.more')"
          severity="secondary"
          class="self-center"
          :loading="list.isFetchingNextPage.value"
          @click="list.fetchNextPage()"
        />
      </template>
    </template>
    <SubmitListingDialog v-model:visible="submitting" />
  </div>
</template>
