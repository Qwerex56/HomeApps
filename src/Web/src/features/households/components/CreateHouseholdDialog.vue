<script setup lang="ts">
import { reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Button from 'primevue/button'
import Message from 'primevue/message'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { householdsApi } from '../api'

// H1: I become the Owner of the new household.
const visible = defineModel<boolean>('visible', { required: true })
const emit = defineEmits<{ created: [id: string] }>()
const { t } = useI18n()
const { busy, formError, fieldErrors, submit } = useSubmit()
const form = reactive({ name: '', description: '' })

async function create() {
  let id = ''
  if (await submit(async () => (id = await householdsApi.create(form.name, form.description.trim() || null)))) {
    visible.value = false
    Object.assign(form, { name: '', description: '' })
    emit('created', id)
  }
}
</script>

<template>
  <Dialog v-model:visible="visible" modal :header="t('households.create')" class="w-full max-w-md">
    <form id="create-household" class="flex flex-col gap-4" novalidate @submit.prevent="create">
      <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
      <FormField id="householdName" :label="t('households.name')" :errors="fieldErrors.name">
        <template #default="{ invalid }">
          <InputText id="householdName" v-model="form.name" :invalid="invalid" autofocus />
        </template>
      </FormField>
      <FormField id="householdDescription" :label="t('households.description')" :errors="fieldErrors.description">
        <template #default="{ invalid }">
          <Textarea id="householdDescription" v-model="form.description" rows="2" auto-resize :invalid="invalid" />
        </template>
      </FormField>
    </form>
    <template #footer>
      <Button :label="t('common.cancel')" severity="secondary" text @click="visible = false" />
      <Button type="submit" form="create-household" :label="t('households.createSubmit')" :loading="busy" />
    </template>
  </Dialog>
</template>
