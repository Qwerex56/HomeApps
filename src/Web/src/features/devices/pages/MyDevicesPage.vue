<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import DeviceCard from '../components/DeviceCard.vue'
import { deviceKeys, devicesApi } from '../api'

// My computers (jarvis-satellites.md S-1d): the bridge and the user app register themselves when they connect.
// Satellites I added belong to their households and are shown there.
const { t } = useI18n()
const { data, error, isPending } = useQuery({ queryKey: deviceKeys.mine, queryFn: devicesApi.mine })
const computers = computed(() => (data.value ?? []).filter((device) => device.householdId === null))
</script>

<template>
  <Card>
    <template #title>{{ t('devices.mine') }}</template>
    <template #subtitle>{{ t('devices.mineHint') }}</template>
    <template #content>
      <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
      <Skeleton v-else-if="isPending" height="4rem" />
      <p v-else-if="!computers.length" class="text-muted-color">{{ t('devices.noComputers') }}</p>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <DeviceCard v-for="device in computers" :key="device.id" :device="device" manage />
      </ul>
    </template>
  </Card>
</template>
