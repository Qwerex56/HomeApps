<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useRouter } from 'vue-router'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys, useInstallations, type Installation, type MixTarget, type Package } from './api'
import AddMixDialog from './AddMixDialog.vue'

// HomeMix add-ons of a household (in its settings, next to the server's plugins) or of my account ("Moje aplikacje").
// Admins and up of a household, or the account's owner, add, update and remove them.
const props = defineProps<{ target: MixTarget; canManage: boolean }>()
const { t, d } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const queryClient = useQueryClient()
const router = useRouter()

const { data: installations } = useInstallations(() => props.target)
const adding = ref(false)
const updating = ref<{ installation: Installation; pkg: Package } | null>(null)
const updateOpen = computed({ get: () => !!updating.value, set: (v) => !v && (updating.value = null) })
const checking = ref<string | null>(null)

function refresh() {
  return queryClient.invalidateQueries({ queryKey: mixKeys.list(props.target) })
}

function failed(error: unknown) {
  toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
}

async function done(installation: Installation) {
  toast.add({
    severity: 'success',
    summary: t('homemix.installed', { name: localized(installation.name) }),
    life: 3000,
  })
  await refresh()
}

// An update that asks for nothing new applies at once; one that asks for more opens the consent screen.
async function update(installation: Installation) {
  checking.value = installation.id
  try {
    const pkg = await homemixApi.addGitHub(installation.repository!)
    if (pkg.version === installation.version) {
      toast.add({ severity: 'info', summary: t('homemix.update.current'), life: 3000 })
      return
    }
    try {
      await homemixApi.update(installation.id, pkg.sha256)
      toast.add({ severity: 'success', summary: t('homemix.update.done', { version: pkg.version }), life: 3000 })
      await refresh()
    } catch (error) {
      if (toProblem(error).code === 'homemix.consent_required') updating.value = { installation, pkg }
      else throw error
    }
  } catch (error) {
    failed(error)
  } finally {
    checking.value = null
  }
}

function open(i: Installation) {
  const path = i.pages[0]?.path ?? ''
  void router.push(
    props.target.kind === 'Household'
      ? { name: 'household-mix', params: { id: props.target.householdId, addon: i.addonId, path } }
      : { name: 'my-mix', params: { addon: i.addonId, path } },
  )
}

function remove(installation: Installation) {
  confirm.require({
    header: t('homemix.remove.title'),
    message: t('homemix.remove.text', { name: localized(installation.name) }),
    acceptProps: { label: t('homemix.remove.title'), severity: 'danger' },
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    accept: async () => {
      try {
        await homemixApi.remove(installation.id)
        await refresh()
      } catch (error) {
        failed(error)
      }
    },
  })
}
</script>

<template>
  <Card>
    <template #title>
      <div class="flex items-center justify-between gap-2">
        <span>{{ t('homemix.title') }}</span>
        <Button
          v-if="canManage"
          :label="t('homemix.add.title')"
          icon="pi pi-plus"
          size="small"
          @click="adding = true"
        />
      </div>
    </template>
    <template #content>
      <p v-if="!installations?.length" class="text-muted-color">{{ t('homemix.empty') }}</p>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li v-for="i in installations" :key="i.id" class="flex flex-wrap items-center gap-3 py-3 first:pt-0 last:pb-0">
          <i :class="['pi', i.icon, 'text-xl text-primary']" aria-hidden="true" />
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2 font-medium">
              {{ localized(i.name) }} <span class="text-sm text-muted-color">{{ i.version }}</span>
              <Tag value="HomeMix" severity="secondary" />
              <Tag v-if="i.jarvisAllowed" value="JARVIS" severity="info" />
            </div>
            <div class="text-sm text-muted-color">
              {{ i.repository ?? t('homemix.fromZip') }} · {{ t('homemix.since', { date: d(i.installedAt, 'short') }) }}
            </div>
          </div>
          <div class="flex gap-1">
            <Button
              v-if="i.pages.length"
              :label="t('homemix.open')"
              icon="pi pi-external-link"
              size="small"
              text
              @click="open(i)"
            />
            <Button
              v-if="canManage && i.repository"
              :label="t('homemix.update.check')"
              icon="pi pi-refresh"
              size="small"
              text
              :loading="checking === i.id"
              @click="update(i)"
            />
            <Button
              v-if="canManage"
              :aria-label="t('homemix.remove.title')"
              icon="pi pi-trash"
              size="small"
              text
              severity="danger"
              @click="remove(i)"
            />
          </div>
        </li>
      </ul>
    </template>
  </Card>
  <AddMixDialog v-model:visible="adding" :target="target" @done="done" />
  <AddMixDialog v-model:visible="updateOpen" :target="target" :update="updating" @done="done" />
</template>
