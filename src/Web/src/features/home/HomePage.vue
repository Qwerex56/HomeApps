<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Avatar from 'primevue/avatar'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { useMyAccount } from '@/features/account/api'
import OwnershipBanner from '@/features/households/components/OwnershipBanner.vue'
import JoinByCode from '@/features/households/components/JoinByCode.vue'
import { householdKeys, householdPermissions, householdsApi } from '@/features/households/api'
import { roleSeverity } from '@/features/households/tags'
import { useCurrentHousehold } from '@/features/households/useCurrentHousehold'

// The dashboard: the current household (Q6) with its people. Device and plugin tiles come after 1.0.
const { t } = useI18n()
const { data: account } = useMyAccount()
const { households, current, isPending } = useCurrentHousehold()
const received = computed(() => households.value?.filter((h) => h.ownershipReceived) ?? [])

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
</script>

<template>
  <div class="flex flex-col gap-6">
    <h1 class="text-2xl font-semibold">{{ t('home.hello', { name: greeting }) }}</h1>

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
          <RouterLink :to="{ name: 'households' }">
            <Button :label="t('households.create')" icon="pi pi-plus" />
          </RouterLink>
        </div>
      </template>
    </Card>
    <JoinByCode v-if="!isPending && !current" />
  </div>
</template>
