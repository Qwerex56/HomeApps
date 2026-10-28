<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Skeleton from 'primevue/skeleton'
import CreateHouseholdDialog from '../components/CreateHouseholdDialog.vue'
import JoinByCode from '../components/JoinByCode.vue'
import { householdKeys } from '../api'
import { roleSeverity } from '../tags'
import { useCurrentHousehold } from '../useCurrentHousehold'

// H2: my households. Every active account may create one (D1) and join others by a code.
const { t } = useI18n()
const router = useRouter()
const queryClient = useQueryClient()
const { households, current, isPending, select } = useCurrentHousehold()
const creating = ref(false)

async function created(id: string) {
  await queryClient.invalidateQueries({ queryKey: householdKeys.mine })
  select(id)
  await router.push({ name: 'household', params: { id } })
}

function open(id: string) {
  select(id)
  router.push({ name: 'household', params: { id } })
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-2xl font-semibold">{{ t('households.title') }}</h1>
      <Button :label="t('households.create')" icon="pi pi-plus" @click="creating = true" />
    </div>

    <div v-if="isPending" class="grid gap-4 sm:grid-cols-2">
      <Skeleton v-for="i in 2" :key="i" height="8rem" />
    </div>
    <Card v-else-if="!households?.length">
      <template #content>
        <div class="flex flex-col items-center gap-3 py-6 text-center">
          <i class="pi pi-home text-4xl text-muted-color" aria-hidden="true" />
          <p class="text-lg">{{ t('households.empty') }}</p>
          <p class="text-muted-color">{{ t('households.emptyHint') }}</p>
        </div>
      </template>
    </Card>
    <div v-else class="grid gap-4 sm:grid-cols-2">
      <button
        v-for="household in households"
        :key="household.id"
        type="button"
        class="rounded-xl border bg-surface-0 p-5 text-left shadow-sm transition hover:border-primary dark:bg-surface-900"
        :class="household.id === current?.id ? 'border-primary' : 'border-surface-200 dark:border-surface-700'"
        @click="open(household.id)"
      >
        <div class="flex items-start justify-between gap-3">
          <div class="min-w-0">
            <div class="truncate text-lg font-semibold">{{ household.name }}</div>
            <div v-if="household.description" class="line-clamp-2 text-sm text-muted-color">
              {{ household.description }}
            </div>
          </div>
          <Tag :value="t(`households.role.${household.myRole}`)" :severity="roleSeverity[household.myRole]" />
        </div>
        <div class="mt-3 flex items-center gap-3 text-sm text-muted-color">
          <span v-if="household.memberCount != null">
            <i class="pi pi-users mr-1" aria-hidden="true" />{{
              t('households.memberCount', Number(household.memberCount))
            }}
          </span>
          <span v-if="household.id === current?.id" class="text-primary">
            <i class="pi pi-check mr-1" aria-hidden="true" />{{ t('households.current') }}
          </span>
        </div>
      </button>
    </div>

    <JoinByCode />
    <CreateHouseholdDialog v-model:visible="creating" @created="created" />
  </div>
</template>
