<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Select from 'primevue/select'
import { householdKeys, householdsApi } from '@/features/households/api'
import { localized } from '@/features/plugins/registry'
import type { Installation, MixTarget, StoreEntry } from '../api'
import AddMixDialog from '../AddMixDialog.vue'

// "Zainstaluj" on an add-on's page: where (a household I administer, or my account, as far as the add-on allows),
// then the usual consent screen (AddMixDialog).
const props = defineProps<{ entry: StoreEntry }>()
const { t } = useI18n()
const toast = useToast()

const households = useQuery({ queryKey: householdKeys.mine, queryFn: householdsApi.mine })
const places = computed(() => {
  const list: { label: string; value: MixTarget }[] = []
  if (props.entry.targets.includes('Household')) {
    for (const h of households.data.value ?? []) {
      if (h.myRole === 'Admin' || h.myRole === 'Owner')
        list.push({ label: h.name, value: { kind: 'Household', householdId: h.id } })
    }
  }
  if (props.entry.targets.includes('User')) list.push({ label: t('homemix.store.myAccount'), value: { kind: 'User' } })
  return list
})
const chosen = ref<MixTarget | null>(null)
const target = computed(() => chosen.value ?? places.value[0]?.value ?? null)
const open = ref(false)

function done(installation: Installation) {
  toast.add({
    severity: 'success',
    summary: t('homemix.store.installed', { name: localized(installation.name) }),
    life: 4000,
  })
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-2">
    <template v-if="places.length">
      <Select
        :model-value="target"
        :options="places"
        option-label="label"
        option-value="value"
        :aria-label="t('homemix.store.installWhere')"
        :placeholder="t('homemix.store.installWhere')"
        @update:model-value="chosen = $event"
      />
      <Button
        :label="t('homemix.store.install')"
        icon="pi pi-download"
        :disabled="!!entry.problem"
        @click="open = true"
      />
      <AddMixDialog v-if="target" v-model:visible="open" :target="target" :entry="entry" @done="done" />
    </template>
    <p v-else-if="!households.isPending.value" class="text-sm text-muted-color">{{ t('homemix.store.noPlace') }}</p>
  </div>
</template>
