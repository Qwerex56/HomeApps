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
import PluginWidgets from '@/features/plugins/PluginWidgets.vue'
import { localized, useHouseholdPlugins } from '@/features/plugins/registry'
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
      name === 'household-settings' ||
      name === 'household-app' ||
      (name === 'household-invites' ? can.value.invite : can.value.seeMembers)
    if (value && !allowed) {
      void router.replace({ name: 'household-settings', params: { id: props.id } })
    }
  },
  { immediate: true },
)

// The plugins that are on here add their pages (manifest.ui.routes with menu) between the household's own tabs.
const { enabled: plugins } = useHouseholdPlugins(() => props.id)
const pluginTabs = computed(() =>
  plugins.value.flatMap((plugin) =>
    (plugin.manifest.ui?.routes ?? [])
      .filter((r) => r.menu)
      .map((r) => ({
        key: `${plugin.manifest.id}/${r.path}`,
        to: { name: 'household-app', params: { id: props.id, plugin: plugin.manifest.id, path: r.path } },
        label: localized(r.title),
        icon: `pi ${plugin.manifest.icon}`,
      })),
  ),
)

const tabs = computed(() => [
  ...(can.value.seeMembers
    ? [tab('household-members', t('households.tabs.members'), 'pi pi-users')]
    : []),
  ...(can.value.invite ? [tab('household-invites', t('households.tabs.invites'), 'pi pi-send')] : []),
  ...pluginTabs.value,
  tab('household-settings', t('households.tabs.settings'), 'pi pi-cog'),
])

function tab(name: string, label: string, icon: string) {
  return { key: name, to: { name, params: { id: props.id } }, label, icon }
}

// Widgets stand on the household's first page: the members, or the settings for a guest, who has no members tab.
const showWidgets = computed(
  () => route.name === (can.value.seeMembers ? 'household-members' : 'household-settings'),
)

const pluginHousehold = computed(() =>
  household.value ? { id: household.value.id, name: household.value.name, myRole: household.value.myRole } : null,
)
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
      <PluginWidgets v-if="pluginHousehold && showWidgets" :household="pluginHousehold" place="Household" />

      <div class="flex flex-col gap-6 md:flex-row">
        <nav :aria-label="household.name" class="flex gap-1 overflow-x-auto md:w-52 md:shrink-0 md:flex-col">
          <RouterLink
            v-for="tab in tabs"
            :key="tab.key"
            :to="tab.to"
            class="flex items-center gap-2 rounded-md px-3 py-2 text-sm whitespace-nowrap text-muted-color hover:bg-emphasis"
            exact-active-class="!text-primary bg-highlight"
          >
            <i :class="tab.icon" aria-hidden="true" />
            <span>{{ tab.label }}</span>
          </RouterLink>
        </nav>
        <section class="min-w-0 flex-1">
          <RouterView :household="household" :can="can" />
        </section>
      </div>
    </template>
  </div>
</template>
