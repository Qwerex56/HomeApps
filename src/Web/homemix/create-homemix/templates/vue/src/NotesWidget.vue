<script setup lang="ts">
import { computed } from 'vue'
import { HmList, HmStat, HmWallTile, HmWidget, useCollection, useWidget } from '@homemix/vue'

// The widget: a click anywhere opens the page (HmWidget); a small one shows only the count, the tablet a big tile.
const notes = useCollection<{ text: string }>('notes')
const { size, isWall } = useWidget()
const items = computed(() => notes.items.value.map((n) => ({ key: n.key, title: n.value.text })))
</script>

<template>
  <HmWallTile v-if="isWall" title="Notatki" icon="📝" :value="items.length" :subtitle="items[0]?.title ?? ''" />
  <HmWidget v-else title="Notatki" icon="📝">
    <HmStat :value="items.length" label="notatek" />
    <HmList v-if="size !== 'Small'" :items="items" empty="Brak notatek" />
  </HmWidget>
</template>
