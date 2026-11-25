<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import Card from 'primevue/card'
import UserAvatar from '@/shared/components/UserAvatar.vue'
import Tag from 'primevue/tag'
import Skeleton from 'primevue/skeleton'
import { householdKeys, householdsApi, type HouseholdRole } from '../api'
import { roleSeverity } from '../tags'

// 2.9: what a guest sees of the others, name, avatar and role badge only; no actions, no dates, no emails.
const props = defineProps<{ householdId: string }>()
const { t } = useI18n()

const { data: names, isPending } = useQuery({
  queryKey: computed(() => householdKeys.names(props.householdId)),
  queryFn: () => householdsApi.names(props.householdId),
})
const order: Record<HouseholdRole, number> = { Owner: 0, Admin: 1, Member: 2, Guest: 3 }
const sorted = computed(() =>
  [...(names.value ?? [])].sort(
    (a, b) => order[a.role ?? 'Member'] - order[b.role ?? 'Member'] || a.name.localeCompare(b.name, 'pl'),
  ),
)
</script>

<template>
  <Card>
    <template #title>{{ t('households.tabs.members') }}</template>
    <template #content>
      <div v-if="isPending" class="flex flex-col gap-3">
        <Skeleton v-for="i in 3" :key="i" height="3rem" />
      </div>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700" data-testid="member-names">
        <li v-for="person in sorted" :key="person.userId" class="flex items-center gap-3 py-3">
          <UserAvatar :src="person.avatarUrl" :name="person.name" />
          <span class="min-w-0 flex-1 truncate font-medium">{{ person.name }}</span>
          <Tag
            v-if="person.role"
            :value="t(`households.role.${person.role}`)"
            :severity="roleSeverity[person.role]"
          />
        </li>
      </ul>
    </template>
  </Card>
</template>
