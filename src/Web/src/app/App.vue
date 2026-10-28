<script setup lang="ts">
import { watch } from 'vue'
import { RouterView, useRoute, useRouter } from 'vue-router'
import Toast from 'primevue/toast'
import ConfirmDialog from 'primevue/confirmdialog'
import ProgressSpinner from 'primevue/progressspinner'
import { useColorScheme } from '@/shared/composables/useColorScheme'
import { useSessionStore } from '@/features/auth/session'

useColorScheme()

const session = useSessionStore()
const route = useRoute()
const router = useRouter()

// Signed out somewhere else (another tab, an expired session): leave the pages that need a session.
watch(
  () => session.status,
  (status) => {
    if (status === 'guest' && route.matched.some((r) => r.meta.requiresAuth)) {
      void router.replace({ name: 'login' })
    }
  },
)
</script>

<template>
  <div v-if="session.status === 'unknown'" class="flex min-h-dvh items-center justify-center">
    <ProgressSpinner style="width: 3rem; height: 3rem" />
  </div>
  <RouterView v-else />
  <Toast position="top-center" />
  <ConfirmDialog />
</template>
