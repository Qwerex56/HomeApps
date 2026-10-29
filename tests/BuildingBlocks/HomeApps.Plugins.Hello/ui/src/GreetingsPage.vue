<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useGreetings } from './useGreetings'

const { host, greetings, load, say } = useGreetings(() => host.household.value!.id)
const text = ref('')

onMounted(load)

async function submit() {
  await say(text.value)
  text.value = ''
}
</script>

<template>
  <div class="hello-page">
    <h2>{{ host.t('title', { name: host.household.value?.name }) }}</h2>
    <form v-if="host.can('greetings.write')" class="hello-form" @submit.prevent="submit">
      <InputText v-model="text" :placeholder="host.t('placeholder')" />
      <Button type="submit" :label="host.t('say')" icon="pi pi-send" />
    </form>
    <p v-else>{{ host.t('readOnly') }}</p>
    <ul v-if="greetings.length">
      <li v-for="greeting in greetings" :key="greeting.id">{{ greeting.text }}</li>
    </ul>
    <p v-else>{{ host.t('none') }}</p>
  </div>
</template>

<style scoped>
.hello-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.hello-form {
  display: flex;
  gap: 0.5rem;
}
</style>
