<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import Message from 'primevue/message'
import Button from 'primevue/button'
import { useSubmit } from '@/shared/composables/useSubmit'
import { householdKeys, householdsApi } from '../api'

// D5: the household came to me because its Owner's account was disabled by an admin or erased.
const props = defineProps<{ householdId: string; name: string }>()
const { t } = useI18n()
const queryClient = useQueryClient()
const { busy, submit } = useSubmit()

async function acknowledge() {
  if (await submit(() => householdsApi.acknowledgeOwnership(props.householdId))) {
    await queryClient.invalidateQueries({ queryKey: householdKeys.all })
  }
}
</script>

<template>
  <Message severity="info" :closable="false">
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <span>{{ t('households.ownership.received', { name }) }}</span>
      <Button :label="t('households.ownership.acknowledge')" size="small" :loading="busy" @click="acknowledge" />
    </div>
  </Message>
</template>
