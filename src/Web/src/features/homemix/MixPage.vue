<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import type { Household } from '@/features/households/api'
import { useInstallations, type MixTarget } from './api'
import MixFrame from './MixFrame.vue'

// /homes/:id/mix/:addon/:path and /me/mix/:addon/:path: an add-on's page (docs/homemix.md).
const props = defineProps<{ household?: Household; addon: string; path?: string }>()
const { t } = useI18n()

const target = computed<MixTarget>(() =>
  props.household ? { kind: 'Household', householdId: props.household.id } : { kind: 'User' },
)
const { data: installations, isPending, error } = useInstallations(target)
const installation = computed(() => installations.value?.find((i) => i.addonId === props.addon))
const frameHousehold = computed(() =>
  props.household?.myRole
    ? { id: props.household.id, name: props.household.name, myRole: props.household.myRole }
    : null,
)
</script>

<template>
  <Skeleton v-if="isPending" height="10rem" />
  <Message v-else-if="error" severity="error" :closable="false">{{ t('homemix.loadFailed') }}</Message>
  <MixFrame
    v-else-if="installation"
    :key="installation.id"
    :installation="installation"
    :household="frameHousehold"
    :page="path ?? ''"
  />
  <Message v-else severity="warn" :closable="false">{{ t('homemix.notInstalled') }}</Message>
</template>
