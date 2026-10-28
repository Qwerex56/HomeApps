<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { useSessionStore } from '@/features/auth/session'
import OwnershipBanner from '../components/OwnershipBanner.vue'
import { householdKeys, householdPermissions, householdsApi } from '../api'
import { roleSeverity } from '../tags'
import { useCurrentHousehold } from '../useCurrentHousehold'

// H3: one household. The tabs below get the household and what my role allows as props.
const props = defineProps<{ id: string }>()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { select } = useCurrentHousehold()

const { data: household, error } = useQuery({
  queryKey: computed(() => householdKeys.one(props.id)),
  queryFn: () => householdsApi.get(props.id),
})
const session = useSessionStore()
const can = computed(() => householdPermissions(household.value?.myRole, session.isAdmin))

// Opening a household makes it the current one (Q6).
watch(
  () => household.value?.myRole,
  (role) => role && select(props.id),
  { immediate: true },
)

// A guest sees no members or invites (D2): only their own membership.
watch(
  () => [household.value, route.name] as const,
  ([value, name]) => {
    const allowed =
      name === 'household-settings' || (name === 'household-invites' ? can.value.invite : can.value.seeMembers)
    if (value && !allowed) {
      void router.replace({ name: 'household-settings', params: { id: props.id } })
    }
  },
  { immediate: true },
)

const tabs = computed(() => [
  ...(can.value.seeMembers
    ? [{ name: 'household-members', label: 'households.tabs.members', icon: 'pi pi-users' }]
    : []),
  ...(can.value.invite ? [{ name: 'household-invites', label: 'households.tabs.invites', icon: 'pi pi-send' }] : []),
  { name: 'household-settings', label: 'households.tabs.settings', icon: 'pi pi-cog' },
])
</script>

<template>
  <div class="flex flex-col gap-6">
    <RouterLink :to="{ name: 'households' }" class="flex items-center gap-2 text-sm text-muted-color hover:underline">
      <i class="pi pi-arrow-left" aria-hidden="true" />{{ t('households.title') }}
    </RouterLink>
    <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
    <Skeleton v-else-if="!household" height="5rem" />
    <template v-else>
      <div class="flex flex-wrap items-start justify-between gap-3">
        <div class="min-w-0">
          <h1 class="text-2xl font-semibold">{{ household.name }}</h1>
          <p v-if="household.description" class="text-muted-color">{{ household.description }}</p>
        </div>
        <div class="flex gap-2">
          <Tag v-if="household.status === 'Archived'" :value="t('households.archived')" severity="danger" />
          <Tag
            v-if="household.myRole"
            :value="t(`households.role.${household.myRole}`)"
            :severity="roleSeverity[household.myRole]"
          />
        </div>
      </div>
      <OwnershipBanner v-if="household.ownershipReceivedAt" :household-id="household.id" :name="household.name" />

      <div class="flex flex-col gap-6 md:flex-row">
        <nav :aria-label="household.name" class="flex gap-1 overflow-x-auto md:w-52 md:shrink-0 md:flex-col">
          <RouterLink
            v-for="tab in tabs"
            :key="tab.name"
            :to="{ name: tab.name, params: { id } }"
            class="flex items-center gap-2 rounded-md px-3 py-2 text-sm whitespace-nowrap text-muted-color hover:bg-emphasis"
            exact-active-class="!text-primary bg-highlight"
          >
            <i :class="tab.icon" aria-hidden="true" />
            <span>{{ t(tab.label) }}</span>
          </RouterLink>
        </nav>
        <section class="min-w-0 flex-1">
          <RouterView :household="household" :can="can" />
        </section>
      </div>
    </template>
  </div>
</template>
