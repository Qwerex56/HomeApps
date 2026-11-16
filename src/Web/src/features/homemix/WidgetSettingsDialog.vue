<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Select from 'primevue/select'
import SelectButton from 'primevue/selectbutton'
import type { WidgetPlace } from '@homemix/sdk'
import { useSubmit } from '@/shared/composables/useSubmit'
import { localized } from '@/features/plugins/registry'
import { homemixApi, mixKeys, type Installation, type MixTarget } from './api'
import { placesOf } from './widgets'

// ⚙ of an add-on's widgets (docs/homemix.md, "Widgets"): for each place it may stand in (a household's page and wall
// tablet, or my page) which widget shows and in which size, all of them as the add-on says, or none.
const props = defineProps<{ installation: Installation | null; target: MixTarget }>()
const visible = defineModel<boolean>('visible', { required: true })
const { t } = useI18n()
const queryClient = useQueryClient()
const { busy, formError, submit } = useSubmit()

const DEFAULT = '__default'
const OFF = '__off'
type Row = { place: WidgetPlace; widget: string; size: string | null }
const rows = ref<Row[]>([])

watch(visible, (open) => {
  if (!open || !props.installation) return
  formError.value = null
  rows.value = placesOf(props.installation).map((place) => {
    const choice = props.installation!.widgetChoices.find((c) => c.place === place)
    return { place, widget: choice ? (choice.widgetId ?? OFF) : DEFAULT, size: choice?.size ?? null }
  })
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
      <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
      <div class="flex justify-end gap-2">
        <Button :label="t('common.cancel')" severity="secondary" text @click="visible = false" />
        <Button type="submit" :label="t('common.save')" :loading="busy" :disabled="!installation?.widgets.length" />
      </div>
    </form>
  </Dialog>
</template>
