<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Avatar from 'primevue/avatar'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { useMyAccount } from '@/features/account/api'
import OwnershipBanner from '@/features/households/components/OwnershipBanner.vue'
import JoinByCode from '@/features/households/components/JoinByCode.vue'
import CreateHouseholdDialog from '@/features/households/components/CreateHouseholdDialog.vue'
import { householdKeys, householdPermissions, householdsApi } from '@/features/households/api'
import { roleSeverity } from '@/features/households/tags'
import { useCurrentHousehold } from '@/features/households/useCurrentHousehold'

// "Domy" (2.4.1, U2): the dashboard and the list of households in one page. The household opened last stands across
// the whole width, my other households are listed under it.
const { t } = useI18n()
const router = useRouter()
const queryClient = useQueryClient()
const { data: account } = useMyAccount()
const { households, current, isPending, select } = useCurrentHousehold()
const received = computed(() => households.value?.filter((h) => h.ownershipReceived) ?? [])
const others = computed(() => households.value?.filter((h) => h.id !== current.value?.id) ?? [])
const creating = ref(false)

const canSeeMembers = computed(() => householdPermissions(current.value?.myRole).seeMembers)
const { data: members } = useQuery({
  queryKey: computed(() => householdKeys.members(current.value?.id ?? '')),
  queryFn: () => householdsApi.members(current.value!.id),
  enabled: canSeeMembers,
})

const greeting = computed(() => {
  const profile = account.value?.profile
  return profile?.preferredName || profile?.firstName || profile?.displayName || ''
})

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
      <h1 class="text-2xl font-semibold">{{ t('home.hello', { name: greeting }) }}</h1>
      <Button :label="t('households.create')" icon="pi pi-plus" @click="creating = true" />
    </div>

    <OwnershipBanner v-for="h in received" :key="h.id" :household-id="h.id" :name="h.name" />

    <Skeleton v-if="isPending" height="10rem" />
    <template v-else-if="current">
      <Card>
        <template #title>
          <div class="flex flex-wrap items-center justify-between gap-2">
            <span>{{ current.name }}</span>
            <Tag :value="t(`households.role.${current.myRole}`)" :severity="roleSeverity[current.myRole]" />
          </div>
        </template>
        <template #subtitle>{{ current.description }}</template>
        <template #content>
          <ul v-if="canSeeMembers" class="flex flex-wrap gap-4">
            <li
              v-for="member in members"
              :key="member.userId"
              class="flex w-20 flex-col items-center gap-1 text-center"
              :class="{ 'opacity-50': !member.isActive }"
            >
              <Avatar v-if="member.avatarUrl" :image="member.avatarUrl" shape="circle" size="large" />
              <Avatar v-else :label="member.name.slice(0, 1).toUpperCase()" shape="circle" size="large" />
              <span class="w-full truncate text-sm">{{ member.name }}</span>
            </li>
          </ul>
          <p v-else class="text-muted-color">{{ t('households.settings.guestInfo') }}</p>
          <div class="mt-4 flex justify-end">
            <RouterLink :to="{ name: 'household', params: { id: current.id } }">
              <Button :label="t('home.openHousehold')" icon="pi pi-arrow-right" icon-pos="right" text />
            </RouterLink>
          </div>
        </template>
      </Card>
    </template>
    <Card v-else>
      <template #content>
        <div class="flex flex-col items-center gap-3 py-6 text-center">
          <i class="pi pi-home text-4xl text-muted-color" aria-hidden="true" />
          <p class="text-lg">{{ t('households.empty') }}</p>
          <p class="text-muted-color">{{ t('households.emptyHint') }}</p>
        </div>
      </template>
    </Card>

    <section v-if="others.length" class="flex flex-col gap-3">
      <h2 class="text-lg font-semibold">{{ t('home.otherHouseholds') }}</h2>
      <div class="grid gap-4 sm:grid-cols-2">
        <button
          v-for="household in others"
          :key="household.id"
          type="button"
          class="rounded-xl border border-surface-200 bg-surface-0 p-5 text-left shadow-sm transition hover:border-primary dark:border-surface-700 dark:bg-surface-900"
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
          <div v-if="household.memberCount != null" class="mt-3 text-sm text-muted-color">
            <i class="pi pi-users mr-1" aria-hidden="true" />{{
              t('households.memberCount', Number(household.memberCount))
            }}
          </div>
        </button>
      </div>
    </section>

    <JoinByCode v-if="!isPending" />
    <CreateHouseholdDialog v-model:visible="creating" @created="created" />
  </div>
</template>
