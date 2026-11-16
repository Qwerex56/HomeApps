<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { homemixApi, mixKeys, type StoreEntry } from '../api'

// A "domek" (homemix-2.md 2.2): one per person and entry, given or taken back with one click.
const props = defineProps<{ entry: StoreEntry }>()
const { t } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()
const busy = ref(false)

async function toggle() {
  busy.value = true
  try {
    await homemixApi.store.heart(props.entry.listingKey, !props.entry.hearted)
    await queryClient.invalidateQueries({ queryKey: mixKeys.store })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <Button
    :icon="entry.hearted ? 'pi pi-home' : 'pi pi-plus'"
    :label="t('homemix.store.hearts', Number(entry.hearts))"
    :aria-label="entry.hearted ? t('homemix.store.takeHeart') : t('homemix.store.giveHeart')"
    :title="entry.hearted ? t('homemix.store.takeHeart') : t('homemix.store.giveHeart')"
    :severity="entry.hearted ? undefined : 'secondary'"
    :outlined="!entry.hearted"
    :loading="busy"
    size="small"
    @click.stop.prevent="toggle"
  />
</template>
