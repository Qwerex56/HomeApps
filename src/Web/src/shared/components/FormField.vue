<script setup lang="ts">
// A labelled form field with the service's validation messages under it (problem.ts fieldErrors).
defineProps<{ id: string; label: string; errors?: string[]; hint?: string }>()
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <label :for="id" class="text-sm font-medium">{{ label }}</label>
    <slot :invalid="!!errors?.length" />
    <small v-for="error in errors" :key="error" class="text-red-500 dark:text-red-400">{{ error }}</small>
    <small v-if="hint && !errors?.length" class="text-muted-color">{{ hint }}</small>
  </div>
</template>
