<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { appsApi, authorizationRequestFrom, type ConsentPreview } from '@/features/account/appsApi'

// The OAuth consent screen (jarvis-v1.md 3.1). Auth's /auth/oauth/authorize sends the browser here with the app's
// query; the router has already made the person sign in. Nothing is decided without a click.
const { t } = useI18n()
const route = useRoute()
const request = authorizationRequestFrom(route.query)

const preview = ref<ConsentPreview | null>(null)
const error = ref<string | null>(null)
const busy = ref<'allow' | 'deny' | null>(null)

onMounted(async () => {
  try {
    preview.value = await appsApi.preview(request)
  } catch (problem) {
    error.value = errorMessage(toProblem(problem))
  }
})

async function decide(allow: boolean) {
  busy.value = allow ? 'allow' : 'deny'
  try {
    window.location.assign(await appsApi.decide(request, allow))
  } catch (problem) {
    error.value = errorMessage(toProblem(problem))
    busy.value = null
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('connect.title') }}</template>
    <template #content>
      <div v-if="error" class="flex flex-col gap-4">
        <Message severity="error" :closable="false">{{ error }}</Message>
        <p class="text-sm text-muted-color">{{ t('connect.errorHint') }}</p>
        <RouterLink :to="{ name: 'home' }" class="text-primary hover:underline">{{ t('connect.backHome') }}</RouterLink>
      </div>
      <div v-else-if="!preview" class="flex justify-center py-6">
        <ProgressSpinner style="width: 2.5rem; height: 2.5rem" />
      </div>
      <div v-else class="flex flex-col gap-4">
        <p class="text-lg">
          <i18n-t keypath="connect.intro" tag="span">
            <template #app>
              <strong>{{ preview.clientName }}</strong>
            </template>
          </i18n-t>
        </p>
        <div>
          <h3 class="mb-1 font-semibold">{{ t('connect.canTitle') }}</h3>
          <ul class="ml-5 list-disc text-muted-color">
            <li>{{ t('connect.can.read') }}</li>
            <li>{{ t('connect.can.apps') }}</li>
          </ul>
        </div>
        <div>
          <h3 class="mb-1 font-semibold">{{ t('connect.cannotTitle') }}</h3>
          <ul class="ml-5 list-disc text-muted-color">
            <li>{{ t('connect.cannot.password') }}</li>
            <li>{{ t('connect.cannot.core') }}</li>
          </ul>
        </div>
        <Message severity="secondary" :closable="false">
          {{ t('connect.unverified', { app: preview.clientName, host: preview.redirectHost }) }}
        </Message>
        <p class="text-sm text-muted-color">{{ t('connect.revokeHint') }}</p>
        <div class="flex flex-col gap-2 sm:flex-row-reverse">
          <Button
            :label="t('connect.allow')"
            class="flex-1"
            :loading="busy === 'allow'"
            :disabled="busy !== null"
            @click="decide(true)"
          />
          <Button
            :label="t('connect.deny')"
            severity="secondary"
            text
            class="flex-1"
            :loading="busy === 'deny'"
            :disabled="busy !== null"
            @click="decide(false)"
          />
        </div>
      </div>
    </template>
  </Card>
</template>
