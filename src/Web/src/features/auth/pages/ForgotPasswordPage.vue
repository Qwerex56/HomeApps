<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { authApi } from '../api'

// A-08: the answer is the same whether the address has an account or not.
const { t } = useI18n()
const { busy, formError, fieldErrors, submit } = useSubmit()
const email = ref('')
const sent = ref(false)

async function send() {
  sent.value = await submit(() => authApi.forgotPassword(email.value))
}
</script>

<template>
  <Card>
    <template #title>{{ t('password.forgot.title') }}</template>
    <template #content>
      <div class="flex flex-col gap-4">
        <Message v-if="sent" severity="success" :closable="false">{{ t('password.forgot.sent', { email }) }}</Message>
        <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="send">
          <p class="text-muted-color">{{ t('password.forgot.text') }}</p>
          <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
          <FormField id="email" :label="t('auth.fields.email')" :errors="fieldErrors.email">
            <template #default="{ invalid }">
              <InputText id="email" v-model="email" type="email" autocomplete="email" :invalid="invalid" autofocus />
            </template>
          </FormField>
          <Button type="submit" :label="t('password.forgot.submit')" :loading="busy" class="w-full" />
        </form>
        <RouterLink :to="{ name: 'login' }" class="text-center text-sm text-primary hover:underline">
          {{ t('auth.callback.back') }}
        </RouterLink>
      </div>
    </template>
  </Card>
</template>
