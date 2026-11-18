<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import FormField from '@/shared/components/FormField.vue'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import type { Household, HouseholdPermissions } from '@/features/households/api'
import DeviceCard from '../components/DeviceCard.vue'
import PairingCodeDialog from '../components/PairingCodeDialog.vue'
import { deviceKeys, devicesApi, type PairingCode } from '../api'

// The household's satellites (S13): every member sees them, admins add one (a pairing code), give a new code, remove
// it and choose "Nie pytaj" for its actions.
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t } = useI18n()
const queryClient = useQueryClient()
const id = computed(() => props.household.id)

const {
  data: devices,
  error,
  isPending,
} = useQuery({
  queryKey: computed(() => deviceKeys.household(id.value)),
  queryFn: () => devicesApi.household(id.value),
})

const code = ref<PairingCode | null>(null)
const name = ref('')
const form = useSubmit()
async function add() {
  const ok = await form.submit(async () => {
    code.value = await devicesApi.addSatellite(id.value, name.value.trim())
  })
  if (!ok) return
  name.value = ''
  await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <Card>
      <template #title>{{ t('devices.household') }}</template>
      <template #subtitle>{{ t('devices.householdHint') }}</template>
      <template #content>
        <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
        <Skeleton v-else-if="isPending" height="4rem" />
        <p v-else-if="!devices?.length" class="text-muted-color">{{ t('devices.noSatellites') }}</p>
        <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
          <DeviceCard
            v-for="device in devices"
            :key="device.id"
            :device="device"
            :manage="can.edit"
            @code="code = $event"
          />
        </ul>
      </template>
    </Card>

    <Card v-if="can.edit">
      <template #title>{{ t('devices.add') }}</template>
      <template #content>
        <form class="flex flex-col gap-4" novalidate @submit.prevent="add">
          <Message v-if="form.formError.value" severity="error" :closable="false">{{ form.formError.value }}</Message>
          <FormField id="satelliteName" :label="t('devices.name')" :errors="form.fieldErrors.value.name">
            <template #default="{ invalid }">
              <InputText
                id="satelliteName"
                v-model="name"
                :placeholder="t('devices.namePlaceholder')"
                maxlength="20"
                :invalid="invalid"
              />
            </template>
          </FormField>
          <div class="flex justify-end">
            <Button
              type="submit"
              :label="t('devices.addButton')"
              icon="pi pi-plus"
              :disabled="!name.trim()"
              :loading="form.busy.value"
            />
          </div>
        </form>
      </template>
    </Card>

    <PairingCodeDialog :code="code" @close="code = null" />
  </div>
</template>
