<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'

const { t } = useI18n()
</script>

<!-- The dashboard: households and shortcuts from F6, tiles of devices and plugins later. -->
<template>
  <Card>
    <template #title>{{ t('home.welcome') }}</template>
    <template #content>
      <p class="text-muted-color">{{ t('home.skeleton') }}</p>
    </template>
  </Card>
</template>
