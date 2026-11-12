<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { HouseholdRole, WidgetPlace } from '@homemix/sdk'
import { useSessionStore } from '@/features/auth/session'
import { useMyAccount } from '@/features/account/api'
import { localized } from '@/features/plugins/registry'
import type { Installation } from './api'
import { useMixHost } from './useMixHost'

// One add-on in its sandbox (ADR 0009): the frame has an opaque origin, no token and no way to navigate the app;
// everything goes through the bridge in useMixHost.
const props = defineProps<{
  installation: Installation
  household: { id: string; name: string; myRole: HouseholdRole } | null
  /** The add-on's page (homemix.json pages[].path); the frame opens the entry with it as #/path. */
  page: string
  /** Another HTML file of the package than the entry, e.g. a widget's own entry. */
  file?: string | null
  widget?: { id: string; place: WidgetPlace } | null
  /** Starting height before the add-on reports its own. */
  height?: number
}>()
const { t } = useI18n()
const session = useSessionStore()
const { data: account } = useMyAccount()

const frame = ref<HTMLIFrameElement>()
const reported = ref<number | null>(null)
const src = computed(() => `${props.installation.runUrl}${props.file ?? ''}${props.page ? `#/${props.page}` : ''}`)

useMixHost(frame, () => ({
  installation: props.installation,
  household: props.household,
  user: { id: session.claims?.sub ?? '', name: account.value?.profile.displayName ?? '' },
  page: props.page,
  widget: props.widget ?? null,
  onResize: (height) => (reported.value = height),
}))
</script>

<template>
  <figure class="flex flex-col gap-1">
    <iframe
      ref="frame"
      :src="src"
      :title="t('homemix.frameTitle', { name: localized(installation.name) })"
      sandbox="allow-scripts allow-forms allow-popups"
      referrerpolicy="no-referrer"
      class="w-full rounded-lg border-0"
      :style="{ height: `${reported ?? height ?? 480}px` }"
    />
    <figcaption class="flex items-center gap-1 text-xs text-muted-color">
      <i class="pi pi-box" aria-hidden="true" />{{ t('homemix.caption', { name: localized(installation.name) }) }}
    </figcaption>
  </figure>
</template>
