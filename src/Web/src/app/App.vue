<script setup lang="ts">
import { RouterView } from 'vue-router'
import Toast from 'primevue/toast'
import ConfirmDialog from 'primevue/confirmdialog'
import { useColorScheme } from '@/shared/composables/useColorScheme'

useColorScheme()
</script>

<template>
  <RouterView />
  <Toast position="top-center" />
  <ConfirmDialog />
</template>
