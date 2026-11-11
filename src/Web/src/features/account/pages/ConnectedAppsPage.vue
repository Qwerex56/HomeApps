<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { appsApi, appsKeys, type ConnectedApp } from '../appsApi'

// "Connected apps" (jarvis-v1.md 3.1): apps acting for me through OAuth, e.g. Claude. Disconnecting ends the app's
// session at once; the access token it still holds stops working within 15 minutes.
const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const { data: apps, isPending } = useQuery({ queryKey: appsKeys.connected, queryFn: appsApi.connected })

function disconnect(app: ConnectedApp) {
  confirm.require({
    header: t('account.apps.disconnect'),
    message: t('account.apps.disconnectText', { app: app.name }),
    acceptProps: { label: t('account.apps.disconnect'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: async () => {
      try {
        await appsApi.disconnect(app.id)
        await queryClient.invalidateQueries({ queryKey: appsKeys.connected })
      } catch (error) {
        toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
      }
    },
  })
}
</script>

<template>
  <Card>
    <template #title>{{ t('account.apps.title') }}</template>
    <template #subtitle>{{ t('account.apps.subtitle') }}</template>
    <template #content>
      <div v-if="isPending" class="flex flex-col gap-3">
        <Skeleton v-for="i in 2" :key="i" height="3rem" />
      </div>
      <p v-else-if="!apps?.length" class="text-muted-color">{{ t('account.apps.empty') }}</p>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li v-for="app in apps" :key="app.id" class="flex items-center justify-between gap-4 py-3">
          <div class="flex min-w-0 items-center gap-3">
            <i class="pi pi-link text-xl text-muted-color" aria-hidden="true" />
            <div class="min-w-0">
              <div class="truncate">{{ app.name }}</div>
              <div class="text-sm text-muted-color">
                {{ t('account.apps.connected', { date: d(new Date(app.connectedAt), 'long') }) }} ·
                {{ t('account.sessions.lastUsed', { date: d(new Date(app.lastUsedAt), 'long') }) }}
              </div>
            </div>
          </div>
          <Button
            icon="pi pi-times"
            :aria-label="t('account.apps.disconnect')"
            v-tooltip.left="t('account.apps.disconnect')"
            severity="secondary"
            text
            rounded
            @click="disconnect(app)"
          />
        </li>
      </ul>
    </template>
  </Card>
</template>
