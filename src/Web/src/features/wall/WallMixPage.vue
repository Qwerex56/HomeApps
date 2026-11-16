<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { useCurrentHousehold } from '@/features/households/useCurrentHousehold'
import { useInstallations, type MixTarget } from '@/features/homemix/api'
import MixFrame from '@/features/homemix/MixFrame.vue'
import { localized } from '@/features/plugins/registry'
import { useWakeLock } from './useWakeLock'

// /wall/mix/:addon/:path: an add-on of the wall's household in full on the tablet, opened from its widget (W2).
const props = defineProps<{ addon: string; path?: string }>()
const { t } = useI18n()
const { current } = useCurrentHousehold()
useWakeLock()

const target = computed<MixTarget | null>(() =>
  current.value ? { kind: 'Household', householdId: current.value.id } : null,
)
const { data: installations, isPending, error } = useInstallations(target)
const installation = computed(() => installations.value?.find((i) => i.addonId === props.addon))
const household = computed(() =>
  current.value?.myRole ? { id: current.value.id, name: current.value.name, myRole: current.value.myRole } : null,
)
</script>

<template>
  <div class="flex min-h-dvh flex-col gap-6 bg-surface-950 p-6 text-surface-0 lg:p-10">
    <header class="flex items-center gap-4">
      <RouterLink :to="{ name: 'wall' }" class="rounded-full p-3 hover:bg-surface-800" :aria-label="t('wall.back')">
        <i class="pi pi-arrow-left text-2xl" aria-hidden="true" />
      </RouterLink>
      <h1 v-if="installation" class="flex items-center gap-3 text-3xl font-semibold">
        <i :class="['pi', installation.icon]" aria-hidden="true" />{{ localized(installation.name) }}
      </h1>
    </header>
    <Skeleton v-if="isPending" height="20rem" />
    <Message v-else-if="error" severity="error" :closable="false">{{ t('homemix.loadFailed') }}</Message>
    <MixFrame
      v-else-if="installation"
      :key="`${installation.id}/${installation.version}`"
      :installation="installation"
      :household="household"
      :page="path ?? ''"
      :height="800"
      wall
    />
    <Message v-else severity="warn" :closable="false">{{ t('homemix.notInstalled') }}</Message>
  </div>
</template>
