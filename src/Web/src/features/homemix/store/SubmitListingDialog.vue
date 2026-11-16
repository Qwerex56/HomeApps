<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useQueryClient } from '@tanstack/vue-query'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { useSubmit } from '@/shared/composables/useSubmit'
import { homemixApi, mixKeys } from '../api'

// Submitting a GitHub repository to the store (homemix-2.md 2.1): it is listed at once and opens on its page.
const visible = defineModel<boolean>('visible', { required: true })
const { t } = useI18n()
const router = useRouter()
const queryClient = useQueryClient()
const { busy, formError, fieldErrors, submit } = useSubmit()
const repository = ref('')

watch(visible, (open) => {
  if (open) {
    repository.value = ''
    formError.value = null
  }
})

async function send() {
  await submit(async () => {
    const entry = await homemixApi.store.submit(repository.value.trim())
    await queryClient.invalidateQueries({ queryKey: mixKeys.store })
    visible.value = false
    await router.push({ name: 'store-entry', params: { listingKey: entry.listingKey } })
  })
}
</script>

<template>
  <Dialog v-model:visible="visible" modal :header="t('homemix.store.submitTitle')" class="w-full max-w-lg">
    <form class="flex flex-col gap-3" @submit.prevent="send">
      <p class="text-sm text-muted-color">{{ t('homemix.store.submitHint') }}</p>
      <InputText
        v-model="repository"
        placeholder="https://github.com/owner/repo"
        :aria-label="t('homemix.store.repository')"
        :invalid="!!fieldErrors.repository"
        autofocus
      />
      <small v-if="fieldErrors.repository" class="text-red-500">{{ fieldErrors.repository[0] }}</small>
      <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
    </form>
    <template #footer>
      <Button :label="t('common.cancel')" severity="secondary" text @click="visible = false" />
      <Button :label="t('homemix.store.submitAccept')" :loading="busy" :disabled="!repository.trim()" @click="send" />
    </template>
  </Dialog>
</template>
