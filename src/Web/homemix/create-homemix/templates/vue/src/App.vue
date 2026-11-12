<script setup lang="ts">
import { ref } from 'vue'
import { useCollection, useHomeMix } from '@homemix/vue'

// A shared list for the household: everyone sees it, Members and up write (homemix.json, storage.household).
const { context, ready } = useHomeMix()
const notes = useCollection<{ text: string }>('notes')
const text = ref('')

async function add() {
  if (!text.value.trim()) return
  await notes.put(crypto.randomUUID(), { text: text.value.trim() })
  text.value = ''
}
</script>

<template>
  <main v-if="ready" class="hm-card">
    <p class="hm-muted">{{ context?.household?.name ?? context?.user.name }}</p>
    <form @submit.prevent="add">
      <input v-model="text" placeholder="Nowa notatka" />
      <button class="hm-button">Dodaj</button>
    </form>
    <ul>
      <li v-for="note in notes.items.value" :key="note.key">
        {{ note.value.text }} <button class="hm-button secondary" @click="notes.remove(note.key)">×</button>
      </li>
    </ul>
  </main>
</template>
