<script setup lang="ts">
import { computed, ref } from 'vue'
import { HmEmpty, HmInput, HmList, HmPage, useCollection, useHomeMix } from '@home-apps/vue'

// The page: a shared list for the household; everyone sees it, Members and up write (homemix.json, storage.household).
const { context } = useHomeMix()
const notes = useCollection<{ text: string }>('notes')
const text = ref('')
const items = computed(() => notes.items.value.map((n) => ({ key: n.key, title: n.value.text })))

async function add() {
  if (!text.value.trim()) return
  await notes.put(crypto.randomUUID(), { text: text.value.trim() })
  text.value = ''
}
</script>

<template>
  <HmPage title="Notatki" :subtitle="context?.household?.name ?? context?.user.name">
    <form class="hm-row" @submit.prevent="add">
      <HmInput v-model="text" class="hm-grow" placeholder="Nowa notatka" />
      <button class="hm-button">Dodaj</button>
    </form>
    <p v-if="notes.error.value" class="hm-badge danger">{{ notes.error.value.message }}</p>
    <div v-if="notes.loading.value" class="hm-loading" />
    <HmEmpty v-else-if="!items.length" icon="📝" text="Brak notatek. Dodaj pierwszą." />
    <HmList v-else :items="items">
      <template #item="{ item }">
        <span class="hm-grow">{{ item.title }}</span>
        <button class="hm-button text" :aria-label="`Usuń ${item.title}`" @click="notes.remove(item.key)">×</button>
      </template>
    </HmList>
  </HmPage>
</template>
