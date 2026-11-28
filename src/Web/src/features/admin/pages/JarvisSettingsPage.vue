<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Card from 'primevue/card'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Message from 'primevue/message'
import Password from 'primevue/password'
import Skeleton from 'primevue/skeleton'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { llmSettingsApi, llmSettingsKeys } from '../llmSettings'

// D14: the model connection of JARVIS, set by an administrator. The key field stays empty: empty keeps the stored key.
const { t } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()

const { data: settings, isPending, isError, error: loadError } = useQuery({
  queryKey: llmSettingsKeys.settings,
  queryFn: () => llmSettingsApi.get(),
})

const baseUrl = ref('')
const model = ref('')
const apiKey = ref('')
const timeoutSeconds = ref(15)
const busy = ref(false)
const result = ref<{ ok: boolean; error: string | null } | null>(null)

watch(
  settings,
  (value) => {
    if (value) {
      baseUrl.value = value.baseUrl
      model.value = value.model
      timeoutSeconds.value = value.timeoutSeconds
    }
  },
  { immediate: true },
)

async function save() {
  busy.value = true
  try {
    const key = apiKey.value.trim()
    const saved = await llmSettingsApi.save({
      baseUrl: baseUrl.value.trim(),
      model: model.value.trim(),
      timeoutSeconds: timeoutSeconds.value,
      ...(key ? { apiKey: key } : {}),
    })
    apiKey.value = ''
    queryClient.setQueryData(llmSettingsKeys.settings, saved)
    toast.add({ severity: 'success', summary: t('admin.jarvis.saved'), life: 3000 })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = false
  }
}

function removeKey() {
  confirm.require({
    header: t('admin.jarvis.removeKey'),
    message: t('admin.jarvis.removeKeyText'),
    acceptProps: { label: t('admin.jarvis.removeKey'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => void doRemoveKey(),
  })
}

async function doRemoveKey() {
  busy.value = true
  try {
    const saved = await llmSettingsApi.save({
      baseUrl: baseUrl.value.trim(),
      model: model.value.trim(),
      timeoutSeconds: timeoutSeconds.value,
      apiKey: '',
    })
    queryClient.setQueryData(llmSettingsKeys.settings, saved)
    toast.add({ severity: 'success', summary: t('admin.jarvis.keyRemoved'), life: 3000 })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = false
  }
}

async function test() {
  busy.value = true
  result.value = null
  try {
    result.value = await llmSettingsApi.test()
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('admin.jarvis.title') }}</template>
    <template #subtitle>{{ t('admin.jarvis.subtitle') }}</template>
    <template #content>
      <Skeleton v-if="isPending" height="10rem" />
      <Message v-else-if="isError" severity="error" :closable="false">{{ errorMessage(toProblem(loadError)) }}</Message>
      <form v-else class="flex max-w-xl flex-col gap-4" @submit.prevent="save">
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('admin.jarvis.baseUrl') }}</span>
          <InputText v-model="baseUrl" placeholder="https://api.openai.com/v1" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('admin.jarvis.model') }}</span>
          <InputText v-model="model" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('admin.jarvis.apiKey') }}</span>
          <Password
            v-model="apiKey"
            :feedback="false"
            toggle-mask
            fluid
            autocomplete="off"
            :placeholder="settings?.apiKeySet ? t('admin.jarvis.keyKept') : ''"
          />
          <Button
            v-if="settings?.apiKeySet"
            type="button"
            :label="t('admin.jarvis.removeKey')"
            text
            size="small"
            class="self-start"
            :disabled="busy"
            @click="removeKey"
          />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('admin.jarvis.timeout') }}</span>
          <InputNumber v-model="timeoutSeconds" :min="1" :max="120" show-buttons />
        </label>
        <div class="flex flex-wrap gap-2">
          <Button type="submit" :label="t('admin.jarvis.save')" :loading="busy" />
          <Button
            type="button"
            :label="t('admin.jarvis.test')"
            severity="secondary"
            outlined
            :disabled="busy"
            @click="test"
          />
        </div>
        <Message v-if="result?.ok" severity="success" :closable="false">{{ t('admin.jarvis.testOk') }}</Message>
        <Message v-else-if="result" severity="error" :closable="false">
          {{ t('admin.jarvis.testFailed', { error: result.error ?? '' }) }}
        </Message>
      </form>
    </template>
  </Card>
</template>
