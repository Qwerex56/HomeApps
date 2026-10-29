<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'

// An invite code typed by hand (ABCDE-FGHJK, case and dash do not matter) or a whole /join link pasted.
const { t } = useI18n()
const router = useRouter()
const input = ref('')
const code = computed(() => {
  const value = input.value.trim()
  const fromLink = value.match(/\/join\/([^/?#\s]+)/)
  return (fromLink ? decodeURIComponent(fromLink[1]!) : value).replace(/[^0-9a-z]/gi, '')
})

function join() {
  if (code.value.length === 10) router.push({ name: 'join', params: { code: code.value.toUpperCase() } })
}
</script>

<template>
  <Card>
    <template #title>{{ t('households.join.title') }}</template>
    <template #subtitle>{{ t('households.join.subtitle') }}</template>
    <template #content>
      <form class="flex flex-col gap-3 sm:flex-row" @submit.prevent="join">
        <InputText
          v-model="input"
          :aria-label="t('households.join.code')"
          placeholder="ABCDE-FGHJK"
          class="flex-1 font-mono uppercase"
        />
        <Button type="submit" :label="t('households.join.next')" :disabled="code.length !== 10" />
      </form>
    </template>
  </Card>
</template>
