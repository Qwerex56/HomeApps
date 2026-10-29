<script setup lang="ts">
import { computed, onMounted } from 'vue'
import type { WidgetProps } from '@homeapps/plugin-sdk'
import { useGreetings } from './useGreetings'

const props = defineProps<WidgetProps>()
const { host, greetings, load } = useGreetings(() => props.householdId)
const latest = computed(() => greetings.value.at(-1)?.text)

onMounted(load)
</script>

<template>
  <p class="hello-latest">{{ latest ?? host.t('none') }}</p>
</template>

<style scoped>
.hello-latest {
  font-size: 1.25rem;
}
</style>
