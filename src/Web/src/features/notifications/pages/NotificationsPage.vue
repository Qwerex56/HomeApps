<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import ToggleSwitch from 'primevue/toggleswitch'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { describeUserAgent } from '@/features/account/securityApi'
import { householdKeys, householdsApi } from '@/features/households/api'
import { localized, usePlugins } from '@/features/plugins/registry'
import { notificationKeys, notificationsApi, type Device } from '../api'
import { currentSubscription, disablePush, enablePush, pushSupport } from '../push'

const { t, d } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()

const { data: settings, isPending } = useQuery({
  queryKey: notificationKeys.settings,
  queryFn: notificationsApi.settings,
})
const { data: households } = useQuery({ queryKey: householdKeys.mine, queryFn: householdsApi.mine })
const { data: plugins } = usePlugins()

/** Plugins that send notifications (manifest "notifications"), so each can be muted per household. */
const notifyingPlugins = computed(() =>
  (plugins.value ?? []).filter((p) => !p.problem && (p.manifest.notifications?.length ?? 0) > 0),
)

const support = pushSupport()
const endpoint = ref<string | null>(null)
const busy = ref(false)

onMounted(async () => {
  endpoint.value = (await currentSubscription())?.endpoint ?? null
})

const permissionDenied = computed(() => support === 'supported' && Notification.permission === 'denied')
const thisDeviceOn = computed(
  () => !!endpoint.value && (settings.value?.devices ?? []).some((device) => device.endpoint === endpoint.value),
)

function fail(error: unknown) {
  toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
}

async function refreshSettings() {
  await queryClient.invalidateQueries({ queryKey: notificationKeys.settings })
}

async function toggleThisDevice(on: boolean) {
  busy.value = true
  try {
    if (on) {
      if (!(await enablePush())) {
        toast.add({ severity: 'warn', summary: t('notifications.device.refused'), life: 6000 })
      }
    } else {
      await disablePush()
    }
    endpoint.value = (await currentSubscription())?.endpoint ?? null
    await refreshSettings()
  } catch (error) {
    fail(error)
  } finally {
    busy.value = false
  }
}

async function sendTest() {
  try {
    const report = await notificationsApi.test()
    toast.add({
      severity: report.delivered ? 'success' : 'warn',
      summary: report.delivered ? t('notifications.test.sent') : t('notifications.test.nothing'),
      life: 4000,
    })
  } catch (error) {
    fail(error)
  }
}

async function removeDevice(device: Device) {
  try {
    if (device.endpoint === endpoint.value) {
      await disablePush()
      endpoint.value = null
    } else {
      await notificationsApi.removeDevice(device.id)
    }
    await refreshSettings()
  } catch (error) {
    fail(error)
  }
}

// Preview and quiet hours: one form, saved together (PUT /notifications/settings).
const showPreview = ref(true)
const quietOn = ref(false)
const quietFrom = ref('22:00')
const quietTo = ref('07:00')
const timeZone = ref(Intl.DateTimeFormat().resolvedOptions().timeZone)

watch(
  settings,
  (value) => {
    if (!value) return
    showPreview.value = value.showPreview
    quietOn.value = !!value.quietHours
    if (value.quietHours) {
      quietFrom.value = value.quietHours.from
      quietTo.value = value.quietHours.to
      timeZone.value = value.quietHours.timeZone
    }
  },
  { immediate: true },
)

const saving = ref(false)
async function saveSettings() {
  saving.value = true
  try {
    await notificationsApi.changeSettings(
      showPreview.value,
      quietOn.value
        ? { from: quietFrom.value, to: quietTo.value, timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone }
        : null,
    )
    await refreshSettings()
    toast.add({ severity: 'success', summary: t('notifications.saved'), life: 3000 })
  } catch (error) {
    fail(error)
  } finally {
    saving.value = false
  }
}

function isMuted(householdId: string, source: string | null) {
  return (settings.value?.mutes ?? []).some((m) => m.householdId === householdId && m.source === source)
}

async function setMuted(householdId: string, source: string | null, muted: boolean) {
  try {
    await (muted ? notificationsApi.mute(householdId, source) : notificationsApi.unmute(householdId, source))
    await refreshSettings()
  } catch (error) {
    fail(error)
  }
}

const icon = (device: Device) =>
  /iPhone|Android|Mobile/.test(device.userAgent ?? '') ? 'pi pi-mobile' : 'pi pi-desktop'
</script>

<template>
  <div class="flex flex-col gap-6">
    <Card>
      <template #title>{{ t('notifications.device.title') }}</template>
      <template #subtitle>{{ t('notifications.device.subtitle') }}</template>
      <template #content>
        <Message v-if="support === 'needs-install'" severity="info" :closable="false">
          {{ t('notifications.device.needsInstall') }}
        </Message>
        <Message v-else-if="support === 'unsupported'" severity="warn" :closable="false">
          {{ t('notifications.device.unsupported') }}
        </Message>
        <template v-else>
          <Message v-if="permissionDenied" severity="warn" :closable="false" class="mb-4">
            {{ t('notifications.device.blocked') }}
          </Message>
          <div class="flex flex-wrap items-center justify-between gap-4">
            <label class="flex items-center gap-3">
              <ToggleSwitch
                :model-value="thisDeviceOn"
                :disabled="busy || isPending || permissionDenied"
                @update:model-value="toggleThisDevice"
              />
              <span>{{ t('notifications.device.toggle') }}</span>
            </label>
            <Button
              :label="t('notifications.test.button')"
              icon="pi pi-bell"
              severity="secondary"
              outlined
              :disabled="!settings?.devices.length"
              @click="sendTest"
            />
          </div>
        </template>
      </template>
    </Card>

    <Card>
      <template #title>{{ t('notifications.settings.title') }}</template>
      <template #content>
        <Skeleton v-if="isPending" height="6rem" />
        <form v-else class="flex flex-col gap-5" @submit.prevent="saveSettings">
          <label class="flex items-start gap-3">
            <ToggleSwitch v-model="showPreview" />
            <span>
              <span class="block">{{ t('notifications.settings.preview') }}</span>
              <span class="block text-sm text-muted-color">{{ t('notifications.settings.previewHint') }}</span>
            </span>
          </label>
          <div class="flex flex-col gap-3">
            <label class="flex items-start gap-3">
              <ToggleSwitch v-model="quietOn" />
              <span>
                <span class="block">{{ t('notifications.settings.quiet') }}</span>
                <span class="block text-sm text-muted-color">{{ t('notifications.settings.quietHint') }}</span>
              </span>
            </label>
            <div v-if="quietOn" class="flex flex-wrap items-end gap-3 pl-14">
              <label class="flex flex-col gap-1 text-sm">
                {{ t('notifications.settings.from') }}
                <InputText v-model="quietFrom" type="time" required />
              </label>
              <label class="flex flex-col gap-1 text-sm">
                {{ t('notifications.settings.to') }}
                <InputText v-model="quietTo" type="time" required />
              </label>
              <span class="pb-2 text-sm text-muted-color">{{ timeZone }}</span>
            </div>
          </div>
          <div class="flex justify-end">
            <Button type="submit" :label="t('common.save')" :loading="saving" />
          </div>
        </form>
      </template>
    </Card>

    <Card v-if="households?.length">
      <template #title>{{ t('notifications.mutes.title') }}</template>
      <template #subtitle>{{ t('notifications.mutes.subtitle') }}</template>
      <template #content>
        <ul class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <li v-for="household in households" :key="household.id" class="flex flex-col gap-2 py-3">
            <label class="flex items-center justify-between gap-4">
              <span class="font-medium">{{ household.name }}</span>
              <ToggleSwitch
                :model-value="!isMuted(household.id, null)"
                :aria-label="t('notifications.mutes.household', { name: household.name })"
                @update:model-value="(on: boolean) => setMuted(household.id, null, !on)"
              />
            </label>
            <label
              v-for="plugin in notifyingPlugins"
              v-show="!isMuted(household.id, null)"
              :key="plugin.manifest.id"
              class="flex items-center justify-between gap-4 pl-4 text-sm"
            >
              <span class="flex items-center gap-2 text-muted-color">
                <i :class="['pi', plugin.manifest.icon]" aria-hidden="true" />
                {{ localized(plugin.manifest.name) }}
              </span>
              <ToggleSwitch
                :model-value="!isMuted(household.id, plugin.manifest.id)"
                @update:model-value="(on: boolean) => setMuted(household.id, plugin.manifest.id, !on)"
              />
            </label>
          </li>
        </ul>
      </template>
    </Card>

    <Card>
      <template #title>{{ t('notifications.devices.title') }}</template>
      <template #subtitle>{{ t('notifications.devices.subtitle') }}</template>
      <template #content>
        <Skeleton v-if="isPending" height="3rem" />
        <p v-else-if="!settings?.devices.length" class="text-muted-color">{{ t('notifications.devices.none') }}</p>
        <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <li v-for="device in settings.devices" :key="device.id" class="flex items-center justify-between gap-4 py-3">
            <div class="flex min-w-0 items-center gap-3">
              <i :class="icon(device)" class="text-xl text-muted-color" aria-hidden="true" />
              <div class="min-w-0">
                <div class="truncate">
                  {{ describeUserAgent(device.userAgent) ?? t('notifications.devices.unknown') }}
                  <span v-if="device.endpoint === endpoint" class="text-sm text-primary">
                    · {{ t('notifications.devices.thisOne') }}
                  </span>
                </div>
                <div class="text-sm text-muted-color">
                  {{ t('notifications.devices.since', { date: d(new Date(device.registeredAt), 'long') }) }}
                </div>
              </div>
            </div>
            <Button
              icon="pi pi-trash"
              :aria-label="t('notifications.devices.remove')"
              v-tooltip.left="t('notifications.devices.remove')"
              severity="secondary"
              text
              rounded
              @click="removeDevice(device)"
            />
          </li>
        </ul>
      </template>
    </Card>
  </div>
</template>
