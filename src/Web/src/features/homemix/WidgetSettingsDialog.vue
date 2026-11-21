<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Select from 'primevue/select'
import SelectButton from 'primevue/selectbutton'
import type { Device, WidgetPlace } from '@home-apps/sdk'
import { useSubmit } from '@/shared/composables/useSubmit'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys, type AddonWidget, type Installation, type MixTarget } from './api'
import { placesOf } from './widgets'

// ⚙ of an add-on's widgets (docs/homemix.md, "Widgets"): for each place it may stand in (a household's page and wall
// tablet, or my page) which widget shows and in which size, all of them as the add-on says, or none; and for widgets
// that match household devices (H-9, ADR item 15: per installation + widget, without place), which device it shows.
const props = defineProps<{ installation: Installation | null; target: MixTarget }>()
const visible = defineModel<boolean>('visible', { required: true })
const { t } = useI18n()
const queryClient = useQueryClient()
const { busy, formError, submit } = useSubmit()

const DEFAULT = '__default'
const OFF = '__off'
const AUTO_DEVICE = '__auto'
type Row = { place: WidgetPlace; widget: string; size: string | null }
type DeviceRow = {
  widgetId: string
  title: string
  devices: Device[]
  needsPick: boolean
  initialPick: string
  picked: string
}
const rows = ref<Row[]>([])
const deviceRows = ref<DeviceRow[]>([])

async function loadDeviceRows(installation: Installation) {
  deviceRows.value = []
  if (installation.targetKind !== 'Household' || !installation.capabilities.includes('devices')) return
  const widgets = (installation.widgets as AddonWidget[]).filter((w) => !w.devices || w.devices.length > 0)
  const loaded: DeviceRow[] = []
  for (const widget of widgets) {
    try {
      const result = await homemixApi.devices.list(installation.id, { widget: widget.id, state: true })
      const savedPick = installation.devicePicks?.find((p) => p.widgetId === widget.id)?.deviceId ?? null
      const initial = savedPick ?? (result.needsPick ? '' : AUTO_DEVICE)
      loaded.push({
        widgetId: widget.id,
        title: localized(widget.title),
        devices: result.devices,
        needsPick: result.needsPick,
        initialPick: initial,
        picked: initial,
      })
    } catch {
      // Widget does not ask for devices (`homemix.widget_without_devices`) or cannot read them: skip.
    }
  }
  deviceRows.value = loaded
}

watch(visible, (open) => {
  if (!open || !props.installation) return
  formError.value = null
  rows.value = placesOf(props.installation).map((place) => {
    const choice = props.installation!.widgetChoices.find((c) => c.place === place)
    return { place, widget: choice ? (choice.widgetId ?? OFF) : DEFAULT, size: choice?.size ?? null }
  })
  void loadDeviceRows(props.installation)
})

function widgetOptions(place: WidgetPlace) {
  return [
    { label: t('homemix.widgets.default'), value: DEFAULT },
    ...(props.installation?.widgets ?? [])
      .filter((w) => w.places.includes(place))
      .map((w) => ({ label: localized(w.title), value: w.id })),
    { label: t('homemix.widgets.off'), value: OFF },
  ]
}

function deviceOptions(row: DeviceRow) {
  const opts = row.devices.map((d) => ({ label: d.name, value: d.id }))
  return row.needsPick ? opts : [{ label: t('homemix.widgets.deviceAuto'), value: AUTO_DEVICE }, ...opts]
}

function sizesOf(row: Row) {
  return props.installation?.widgets.find((w) => w.id === row.widget)?.sizes ?? []
}

function pick(row: Row) {
  const widget = props.installation?.widgets.find((w) => w.id === row.widget)
  row.size = widget ? widget.size : null
}

function sizeOptions(row: Row) {
  return sizesOf(row).map((s) => ({ label: t(`homemix.widgets.size.${s}`), value: s }))
}

async function save() {
  const installation = props.installation!
  const ok = await submit(async () => {
    for (const row of rows.value) {
      if (row.widget === DEFAULT) await homemixApi.resetWidget(installation.id, row.place)
      else if (row.widget === OFF) await homemixApi.chooseWidget(installation.id, row.place, null, null)
      else await homemixApi.chooseWidget(installation.id, row.place, row.widget, row.size)
    }
    for (const dRow of deviceRows.value) {
      if (dRow.picked === dRow.initialPick) continue
      const deviceId = !dRow.picked || dRow.picked === AUTO_DEVICE ? null : dRow.picked
      await homemixApi.devices.chooseWidgetDevice(installation.id, dRow.widgetId, deviceId)
    }
  })
  await queryClient.invalidateQueries({ queryKey: mixKeys.list(props.target) })
  if (ok) visible.value = false
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    modal
    :header="t('homemix.widgets.title', { name: installation ? localized(installation.name) : '' })"
    class="w-full max-w-lg"
  >
    <form class="flex flex-col gap-5" @submit.prevent="save">
      <p v-if="!installation?.widgets.length" class="text-muted-color">{{ t('homemix.widgets.none') }}</p>
      <fieldset v-for="row in rows" :key="row.place" class="flex flex-col gap-2">
        <legend class="mb-2 font-medium">{{ t(`homemix.widgets.place.${row.place}`) }}</legend>
        <Select
          v-model="row.widget"
          :options="widgetOptions(row.place)"
          option-label="label"
          option-value="value"
          :aria-label="t(`homemix.widgets.place.${row.place}`)"
          @change="pick(row)"
        />
        <SelectButton
          v-if="sizesOf(row).length > 1"
          v-model="row.size"
          :options="sizeOptions(row)"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          :aria-label="t('homemix.widgets.sizeLabel')"
        />
      </fieldset>

      <fieldset
        v-for="dRow in deviceRows"
        :key="`device-${dRow.widgetId}`"
        class="flex flex-col gap-2"
        :data-testid="`widget-device-${dRow.widgetId}`"
      >
        <legend class="mb-1 font-medium">{{ t('homemix.widgets.deviceLabel', { widget: dRow.title }) }}</legend>
        <p v-if="!dRow.devices.length" class="text-sm text-muted-color">{{ t('homemix.widgets.noMatches') }}</p>
        <template v-else>
          <p v-if="dRow.needsPick" class="text-xs text-muted-color">{{ t('homemix.widgets.needsPick') }}</p>
          <Select
            v-model="dRow.picked"
            :options="deviceOptions(dRow)"
            option-label="label"
            option-value="value"
            :placeholder="t('homemix.widgets.devicePick')"
            :aria-label="t('homemix.widgets.deviceLabel', { widget: dRow.title })"
          />
        </template>
      </fieldset>

      <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
      <div class="flex justify-end gap-2">
        <Button :label="t('common.cancel')" severity="secondary" text @click="visible = false" />
        <Button type="submit" :label="t('common.save')" :loading="busy" :disabled="!installation?.widgets.length" />
      </div>
    </form>
  </Dialog>
</template>

