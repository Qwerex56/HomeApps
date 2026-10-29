<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'

const { t } = useI18n()
</script>

<template>
  <Card>
    <template #title>{{ t('app.notFound') }}</template>
    <template #content>
      <RouterLink :to="{ name: 'home' }" class="text-primary hover:underline">{{ t('app.backHome') }}</RouterLink>
    </template>
  </Card>
</template>
