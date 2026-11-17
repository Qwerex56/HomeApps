<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import { useSubmit } from '@/shared/composables/useSubmit'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys } from './api'
import { MAX_PINS, useMyApps } from './myApps'

// ✎ of the quick links bar: which of my account's add-ons stand on it (at most 8) and in which order.
const visible = defineModel<boolean>('visible', { required: true })
const { t } = useI18n()
const queryClient = useQueryClient()
const { busy, formError, submit } = useSubmit()
const { apps, pinned } = useMyApps()

// The pinned ones first, in their order, then the rest.
const order = ref<string[]>([])
const chosen = ref<string[]>([])
watch(visible, (open) => {
  if (!open) return
  formError.value = null
  chosen.value = pinned.value.map((a) => a.id)
  order.value = [...chosen.value, ...apps.value.filter((a) => a.pin == null).map((a) => a.id)]
})

function app(id: string) {
  return apps.value.find((a) => a.id === id)
}

function move(id: string, by: -1 | 1) {
  const at = order.value.indexOf(id)
  const to = at + by
  if (to < 0 || to >= order.value.length) return
  const next = [...order.value]
  ;[next[at], next[to]] = [next[to]!, next[at]!]
  order.value = next
}

async function save() {
  const ids = order.value.filter((id) => chosen.value.includes(id))
  const ok = await submit(() => homemixApi.pin(ids))
  await queryClient.invalidateQueries({ queryKey: mixKeys.list({ kind: 'User' }) })
  if (ok) visible.value = false
}
</script>

<template>
  <Dialog v-model:visible="visible" modal :header="t('quickLinks.title')" class="w-full max-w-md">
    <form class="flex flex-col gap-4" @submit.prevent="save">
      <p class="text-sm text-muted-color">{{ t('quickLinks.hint', { max: MAX_PINS }) }}</p>
      <ul class="flex flex-col gap-1">
        <li v-for="id in order" :key="id" class="flex items-center gap-3 rounded-md px-2 py-1 hover:bg-emphasis">
          <Checkbox
            v-model="chosen"
            :value="id"
            :input-id="`pin-${id}`"
            :disabled="!chosen.includes(id) && chosen.length >= MAX_PINS"
          />
          <label :for="`pin-${id}`" class="flex min-w-0 flex-1 items-center gap-2">
            <i :class="['pi', app(id)?.icon]" aria-hidden="true" />
            <span class="truncate">{{ app(id) ? localized(app(id)!.name) : '' }}</span>
          </label>
          <Button
            icon="pi pi-arrow-up"
            size="small"
            text
            rounded
            severity="secondary"
            :aria-label="t('quickLinks.up')"
            @click="move(id, -1)"
          />
          <Button
            icon="pi pi-arrow-down"
            size="small"
            text
            rounded
            severity="secondary"
            :aria-label="t('quickLinks.down')"
            @click="move(id, 1)"
          />
        </li>
      </ul>
      <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
      <div class="flex justify-end gap-2">
        <Button :label="t('common.cancel')" severity="secondary" text @click="visible = false" />
        <Button type="submit" :label="t('common.save')" :loading="busy" />
      </div>
    </form>
  </Dialog>
</template>
