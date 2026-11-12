<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import RadioButton from 'primevue/radiobutton'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys, type AdminInstallation } from '@/features/homemix/api'

// A5: the Owner sees every HomeMix installation and blocks an add-on (every version by id, or one package by
// SHA-256). Blocks from HomeMix:Blocked are shown but lifted only in the configuration.
const { t, d } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()

const installations = useQuery({ queryKey: mixKeys.adminInstallations, queryFn: homemixApi.admin.installations })
const blocks = useQuery({ queryKey: mixKeys.blocks, queryFn: homemixApi.admin.blocks })

const blocking = ref<AdminInstallation | null>(null)
const what = ref<'id' | 'sha'>('id')
const reason = ref('')
const manual = ref('')
const busy = ref(false)
const visible = computed({ get: () => !!blocking.value, set: (v) => !v && (blocking.value = null) })

function startBlock(installation: AdminInstallation) {
  blocking.value = installation
  what.value = 'id'
  reason.value = ''
}

async function run(action: () => Promise<unknown>, done: string) {
  busy.value = true
  try {
    await action()
    toast.add({ severity: 'success', summary: done, life: 3000 })
    await queryClient.invalidateQueries({ queryKey: ['homemix'] })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = false
  }
}

async function block() {
  const i = blocking.value!
  const value = what.value === 'id' ? i.addonId : i.sha256
  await run(() => homemixApi.admin.block(value, reason.value.trim() || null), t('admin.addons.blockedDone', { value }))
  blocking.value = null
}

async function blockManual() {
  const value = manual.value.trim()
  await run(() => homemixApi.admin.block(value, null), t('admin.addons.blockedDone', { value }))
  manual.value = ''
}

const unblock = (value: string) =>
  run(() => homemixApi.admin.unblock(value), t('admin.addons.unblockedDone', { value }))

const short = (value: string) => (value.length === 64 ? `${value.slice(0, 12)}…` : value)
</script>

<template>
  <div class="flex flex-col gap-4">
    <Card>
      <template #title>{{ t('admin.addons.title') }}</template>
      <template #subtitle>{{ t('admin.addons.subtitle') }}</template>
      <template #content>
        <Skeleton v-if="installations.isPending.value" height="6rem" />
        <Message v-else-if="!installations.data.value?.length" severity="secondary" :closable="false">
          {{ t('admin.addons.empty') }}
        </Message>
        <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <li
            v-for="i in installations.data.value"
            :key="i.id"
            class="flex flex-wrap items-center gap-3 py-3 first:pt-0 last:pb-0"
          >
            <div class="min-w-0 flex-1">
              <div class="font-medium">
                {{ localized(i.name) }} <span class="text-sm text-muted-color">{{ i.addonId }} {{ i.version }}</span>
              </div>
              <div class="text-sm break-all text-muted-color">
                {{
                  i.targetKind === 'Household'
                    ? t('admin.addons.household', { id: i.targetId })
                    : t('admin.addons.account', { id: i.targetId })
                }}
                · {{ d(i.installedAt, 'short') }} · {{ i.repository ?? short(i.sha256) }}
              </div>
              <div v-if="i.network.length" class="text-xs text-muted-color">{{ i.network.join(', ') }}</div>
            </div>
            <Tag v-if="i.blocked" :value="t('admin.addons.blocked')" severity="danger" />
            <Button
              v-else
              :label="t('admin.addons.block')"
              severity="danger"
              size="small"
              outlined
              @click="startBlock(i)"
            />
          </li>
        </ul>
      </template>
    </Card>

    <Card>
      <template #title>{{ t('admin.addons.blocks') }}</template>
      <template #content>
        <Skeleton v-if="blocks.isPending.value" height="4rem" />
        <p v-else-if="!blocks.data.value?.length" class="text-muted-color">{{ t('admin.addons.noBlocks') }}</p>
        <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <li v-for="b in blocks.data.value" :key="b.value" class="flex flex-wrap items-center gap-3 py-2">
            <div class="min-w-0 flex-1">
              <div class="font-mono text-sm break-all">{{ b.value }}</div>
              <div class="text-sm text-muted-color">
                <template v-if="b.fromConfiguration">{{ t('admin.addons.fromConfiguration') }}</template>
                <template v-else>{{ b.blockedAt ? d(b.blockedAt, 'short') : '' }} {{ b.reason ?? '' }}</template>
              </div>
            </div>
            <Button
              v-if="!b.fromConfiguration"
              :label="t('admin.addons.unblock')"
              size="small"
              text
              :loading="busy"
              @click="unblock(b.value)"
            />
          </li>
        </ul>
        <form class="mt-4 flex flex-wrap gap-2" @submit.prevent="blockManual">
          <InputText
            v-model="manual"
            class="min-w-0 flex-1"
            :placeholder="t('admin.addons.valuePlaceholder')"
            :aria-label="t('admin.addons.addBlock')"
          />
          <Button type="submit" :label="t('admin.addons.addBlock')" :disabled="!manual.trim()" :loading="busy" />
        </form>
      </template>
    </Card>

    <Dialog
      v-model:visible="visible"
      modal
      :header="blocking ? t('admin.addons.blockTitle', { name: localized(blocking.name) }) : ''"
      class="w-full max-w-lg"
    >
      <div v-if="blocking" class="flex flex-col gap-3">
        <p>{{ t('admin.addons.blockText') }}</p>
        <label class="flex items-center gap-2">
          <RadioButton v-model="what" value="id" />
          {{ t('admin.addons.blockId', { id: blocking.addonId }) }}
        </label>
        <label class="flex items-center gap-2">
          <RadioButton v-model="what" value="sha" />
          {{ t('admin.addons.blockSha', { version: blocking.version }) }}
        </label>
        <InputText v-model="reason" :placeholder="t('admin.addons.reason')" :aria-label="t('admin.addons.reason')" />
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" severity="secondary" text @click="blocking = null" />
        <Button :label="t('admin.addons.block')" severity="danger" :loading="busy" @click="block" />
      </template>
    </Dialog>
  </div>
</template>
