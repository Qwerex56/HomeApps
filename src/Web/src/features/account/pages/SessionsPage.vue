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
import { useSessionStore } from '@/features/auth/session'
import { describeUserAgent, securityApi, securityKeys, type Session } from '../securityApi'

const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const session = useSessionStore()
const { data: sessions, isPending } = useQuery({ queryKey: securityKeys.sessions, queryFn: securityApi.sessions })

const icon = (item: Session) => (/iPhone|Android|Mobile/.test(item.userAgent ?? '') ? 'pi pi-mobile' : 'pi pi-desktop')

async function revoke(item: Session) {
  try {
    await securityApi.revokeSession(item.id)
    await queryClient.invalidateQueries({ queryKey: securityKeys.sessions })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}

function signOutEverywhere() {
  confirm.require({
    header: t('auth.logoutEverywhere'),
    message: t('account.sessions.everywhereText'),
    acceptProps: { label: t('auth.logoutEverywhere'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: () => session.signOut(true),
  })
}
</script>

<template>
  <Card>
    <template #title>{{ t('account.sessions.title') }}</template>
    <template #subtitle>{{ t('account.sessions.subtitle') }}</template>
    <template #content>
      <div v-if="isPending" class="flex flex-col gap-3">
        <Skeleton v-for="i in 3" :key="i" height="3rem" />
      </div>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li v-for="item in sessions" :key="item.id" class="flex items-center justify-between gap-4 py-3">
          <div class="flex min-w-0 items-center gap-3">
            <i :class="icon(item)" class="text-xl text-muted-color" aria-hidden="true" />
            <div class="min-w-0">
              <div class="truncate">{{ describeUserAgent(item.userAgent) ?? item.clientId }}</div>
              <div class="text-sm text-muted-color">
                {{ t(`account.sessions.method.${item.method}`) }} ·
                {{ t('account.sessions.lastUsed', { date: d(new Date(item.lastUsedAt), 'long') }) }}
              </div>
            </div>
          </div>
          <Button
            icon="pi pi-sign-out"
            :aria-label="t('account.sessions.revoke')"
            v-tooltip.left="t('account.sessions.revoke')"
            severity="secondary"
            text
            rounded
            @click="revoke(item)"
          />
        </li>
      </ul>
      <div class="mt-4 flex justify-end">
        <Button
          :label="t('auth.logoutEverywhere')"
          icon="pi pi-power-off"
          severity="danger"
          outlined
          @click="signOutEverywhere"
        />
      </div>
    </template>
  </Card>
</template>
