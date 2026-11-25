<script setup lang="ts">
import { ref, watch } from 'vue'
import Avatar from 'primevue/avatar'

// 2.15: the picture, or the initial when there is none or the address answers 404 (no uploaded file).
const props = defineProps<{ src?: string | null; name: string; size?: 'normal' | 'large' | 'xlarge' }>()
const failed = ref(false)
watch(() => props.src, () => (failed.value = false))
</script>

<template>
  <Avatar v-if="src && !failed" :image="src" :size="size" shape="circle" @error="failed = true" />
  <Avatar
    v-else
    :label="name.slice(0, 1).toUpperCase()"
    :size="size"
    shape="circle"
    class="!bg-primary !text-primary-contrast"
    data-testid="avatar-fallback"
  />
</template>
