<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useI18n } from 'vue-i18n'
import ColorSchemeToggle from '@/shared/components/ColorSchemeToggle.vue'

const { t } = useI18n()
</script>

<!-- Sign-in, registration and the email links (F2, F3): one centered card, no menu. -->
<template>
  <div class="flex min-h-dvh flex-col items-center justify-center gap-6 px-4 py-10">
    <div class="absolute top-3 right-3"><ColorSchemeToggle /></div>
    <div class="flex items-center gap-2 text-2xl font-semibold text-primary">
      <i class="pi pi-home text-2xl" aria-hidden="true" />
      <span>{{ t('app.name') }}</span>
    </div>
    <div class="w-full max-w-md">
      <RouterView />
    </div>
  </div>
</template>
