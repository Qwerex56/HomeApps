<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import { googleSignInUrl } from './api'

const props = defineProps<{ invitation?: string }>()
const { t } = useI18n()

// A full page navigation, not a request: Google's pages cannot open inside the app.
function go() {
  window.location.assign(googleSignInUrl(props.invitation))
}
</script>

<template>
  <Button :label="t('auth.google')" icon="pi pi-google" severity="secondary" outlined class="w-full" @click="go" />
</template>
