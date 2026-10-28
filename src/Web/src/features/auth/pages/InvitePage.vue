<script setup lang="ts">
import { useAfterSignIn } from '../useAfterSignIn'
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Divider from 'primevue/divider'
import ProgressSpinner from 'primevue/progressspinner'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { authApi, type InvitationPreview } from '../api'
import { useSessionStore } from '../session'
import GoogleButton from '../GoogleButton.vue'

// A-07: the link from the invitation email. The account is made here with a password, or with Google carrying the
// invitation (D8: the Google email may differ from the invited one).
const { t, d } = useI18n()
const route = useRoute()
const afterSignIn = useAfterSignIn()
const session = useSessionStore()
const { busy, formError, fieldErrors, submit } = useSubmit()

const token = computed(() => String(route.params.token))
const preview = ref<InvitationPreview | null>(null)
const loadError = ref<string | null>(null)
const form = reactive({ displayName: '', firstName: '', lastName: '', password: '' })

onMounted(async () => {
  try {
    preview.value = await authApi.invitation(token.value)
  } catch (error) {
    loadError.value = errorMessage(toProblem(error))
  }
})

async function accept() {
  const ok = await submit(async () =>
    session.apply(
      await authApi.acceptInvitation(token.value, {
        password: form.password,
        displayName: form.displayName,
        firstName: form.firstName || null,
        lastName: form.lastName || null,
        locale: 'pl',
      }),
    ),
  )
  if (ok) {
    await afterSignIn()
  }
}
</script>

<template>
  <Card>
    <template #title>{{ t('invite.title') }}</template>
    <template #content>
      <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
      <div v-else-if="!preview" class="flex justify-center py-6">
        <ProgressSpinner style="width: 2.5rem; height: 2.5rem" />
      </div>
      <div v-else-if="session.status === 'signedIn'" class="flex flex-col gap-4">
        <p>{{ t('invite.signedIn', { email: session.claims?.email }) }}</p>
        <Button :label="t('auth.logout')" severity="secondary" @click="session.signOut()" />
      </div>
      <div v-else class="flex flex-col gap-4">
        <p class="text-muted-color">
          <template v-if="preview.kind === 'Bootstrap'">{{ t('invite.bootstrap') }}</template>
          <template v-else-if="preview.invitedByEmail">{{ t('invite.by', { by: preview.invitedByEmail }) }}</template>
          {{ t('invite.for', { email: preview.email, date: d(new Date(preview.expiresAt), 'short') }) }}
        </p>
        <GoogleButton :invitation="token" />
        <Divider align="center"
          ><span class="text-sm text-muted-color">{{ t('invite.orPassword') }}</span></Divider
        >
        <form class="flex flex-col gap-4" novalidate @submit.prevent="accept">
          <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
          <FormField id="displayName" :label="t('auth.fields.displayName')" :errors="fieldErrors.displayName">
            <template #default="{ invalid }">
              <InputText id="displayName" v-model="form.displayName" autocomplete="nickname" :invalid="invalid" />
            </template>
          </FormField>
          <div class="grid gap-4 sm:grid-cols-2">
            <FormField id="firstName" :label="t('auth.fields.firstName')" :errors="fieldErrors.firstName">
              <template #default="{ invalid }">
                <InputText id="firstName" v-model="form.firstName" autocomplete="given-name" :invalid="invalid" />
              </template>
            </FormField>
            <FormField id="lastName" :label="t('auth.fields.lastName')" :errors="fieldErrors.lastName">
              <template #default="{ invalid }">
                <InputText id="lastName" v-model="form.lastName" autocomplete="family-name" :invalid="invalid" />
              </template>
            </FormField>
          </div>
          <FormField id="password" :label="t('auth.fields.password')" :errors="fieldErrors.password">
            <template #default="{ invalid }">
              <Password
                v-model="form.password"
                input-id="password"
                :feedback="false"
                toggle-mask
                fluid
                :input-props="{ autocomplete: 'new-password' }"
                :invalid="invalid"
              />
            </template>
          </FormField>
          <Button type="submit" :label="t('invite.submit')" :loading="busy" class="w-full" />
        </form>
      </div>
    </template>
  </Card>
</template>
