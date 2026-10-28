<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Select from 'primevue/select'
import { useCurrentHousehold } from '../useCurrentHousehold'

// Q6: the household the app shows, in the header. Hidden until I have at least two.
const { t } = useI18n()
const router = useRouter()
const { households, current, select } = useCurrentHousehold()

function change(id: string) {
  select(id)
  // On a household page, follow the choice to the other household.
  const route = router.currentRoute.value
  if (typeof route.name === 'string' && route.name.startsWith('household-')) {
    void router.push({ name: route.name, params: { id } })
  }
}
</script>

<template>
  <Select
    v-if="(households?.length ?? 0) > 1"
    :model-value="current?.id"
    :options="households"
    option-label="name"
    option-value="id"
    :aria-label="t('households.switch')"
    class="max-w-full sm:max-w-48"
    size="small"
    @update:model-value="change"
  >
    <template #value="{ value }">
      <span class="flex items-center gap-2 truncate">
        <i class="pi pi-home" aria-hidden="true" />
        <span class="truncate">{{ households?.find((h) => h.id === value)?.name }}</span>
      </span>
    </template>
  </Select>
</template>
