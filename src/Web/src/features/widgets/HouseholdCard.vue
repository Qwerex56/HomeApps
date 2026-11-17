<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { householdKeys, householdPermissions, householdsApi } from '@/features/households/api'
import type { WidgetHousehold } from './board'

// The household card as a widget (UI 2.4.1): the household's name and its people. A guest sees only the name (D2).
const props = defineProps<{ household: WidgetHousehold; dark?: boolean }>()
const { t } = useI18n()

const canSeeMembers = computed(() => householdPermissions(props.household.myRole).seeMembers)
const { data: members } = useQuery({
  queryKey: computed(() => householdKeys.members(props.household.id)),
  queryFn: () => householdsApi.members(props.household.id),
  enabled: canSeeMembers,
  // On the wall a new person or a name change shows up without a reload.
  refetchInterval: computed(() => (props.dark ? 5 * 60_000 : false)),
})
const people = computed(() => (members.value ?? []).filter((m) => m.isActive))
</script>

<template>
  <div class="flex h-full min-h-0 flex-col gap-3 overflow-hidden">
    <h2 class="truncate font-semibold" :class="dark ? 'text-4xl' : 'text-xl'">{{ household.name }}</h2>
    <ul v-if="canSeeMembers" class="flex min-h-0 flex-wrap content-start gap-3 overflow-hidden">
      <li
        v-for="member in people"
        :key="member.userId"
        class="flex flex-col items-center gap-1 text-center"
        :class="dark ? 'w-24' : 'w-16'"
      >
        <img
          v-if="member.avatarUrl"
          :src="member.avatarUrl"
          alt=""
          class="rounded-full object-cover"
          :class="dark ? 'size-20' : 'size-12'"
        />
        <span
          v-else
          class="flex items-center justify-center rounded-full bg-primary font-semibold text-primary-contrast"
          :class="dark ? 'size-20 text-3xl' : 'size-12 text-lg'"
          aria-hidden="true"
          >{{ member.name.slice(0, 1).toUpperCase() }}</span
        >
        <span class="w-full truncate" :class="dark ? 'text-lg' : 'text-xs'">{{ member.name }}</span>
      </li>
    </ul>
    <p v-else class="text-sm text-muted-color">{{ t('households.settings.guestInfo') }}</p>
  </div>
</template>
