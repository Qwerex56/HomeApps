<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem, type Problem } from '@/api/problem'
import { formatCode, householdKeys, householdsApi } from '../api'
import { roleSeverity } from '../tags'
import { useCurrentHousehold } from '../useCurrentHousehold'

// I4, I5: a personal invite or the guest link. A signed-out visitor signs in or up first and comes back here.
const props = defineProps<{ code: string }>()
const { t, d } = useI18n()
const router = useRouter()
const queryClient = useQueryClient()
const { select } = useCurrentHousehold()

const { data: preview, error } = useQuery({
  queryKey: computed(() => householdKeys.preview(props.code)),
  queryFn: () => householdsApi.preview(props.code),
  retry: false,
})
const { busy, formError, submit } = useSubmit()

async function open(id: string) {
  await queryClient.invalidateQueries({ queryKey: householdKeys.mine })
  select(id)
  await router.replace({ name: 'household', params: { id } })
}

// Already a member: nothing to accept, just open the household.
function alreadyMember(problem: Problem) {
  if (problem.code === 'household.already_member' && preview.value) {
    void open(preview.value.householdId)
    return true
  }
}

async function accept() {
  let id = ''
  if (await submit(async () => (id = await householdsApi.accept(props.code)), alreadyMember)) await open(id)
}
</script>

<template>
  <div class="mx-auto flex max-w-md flex-col gap-4">
    <Card>
      <template #title>{{ t('households.join.title') }}</template>
      <template #content>
        <Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
        <Skeleton v-else-if="!preview" height="6rem" />
        <div v-else class="flex flex-col gap-4">
          <p>
            {{
              preview.invitedByName
                ? t('households.join.invitedBy', { name: preview.invitedByName })
                : t('households.join.guestLink')
            }}
          </p>
          <div class="rounded-lg bg-emphasis p-4">
            <div class="text-xl font-semibold">{{ preview.householdName }}</div>
            <div class="mt-2 flex items-center gap-2 text-sm text-muted-color">
              {{ t('households.join.as') }}
              <Tag :value="t(`households.role.${preview.role}`)" :severity="roleSeverity[preview.role]" />
            </div>
            <div v-if="preview.expiresAt" class="mt-1 text-sm text-muted-color">
              {{ t('households.join.expires', { date: d(new Date(preview.expiresAt), 'long') }) }}
            </div>
          </div>
          <p v-if="preview.role === 'Guest'" class="text-sm text-muted-color">{{ t('households.join.guestHint') }}</p>
          <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
          <Button :label="t('households.join.accept')" icon="pi pi-sign-in" :loading="busy" @click="accept" />
        </div>
        <p class="mt-4 text-center font-mono text-sm text-muted-color">{{ formatCode(code) }}</p>
      </template>
    </Card>
  </div>
</template>
