<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { jarvisApi, type Action, type ActionStatus } from './api'

// What JARVIS did (jarvis-v1.md 5.4): one line per change; one that waits gets Confirm / Cancel for 10 minutes.
const props = defineProps<{ actions: Action[]; readonly?: boolean }>()
const emit = defineEmits<{ decided: [action: Action] }>()

const { t, d } = useI18n()
const toast = useToast()
const busy = ref<string | null>(null)

const icons: Record<ActionStatus, string> = {
  Done: 'pi pi-check-circle text-green-600',
  Failed: 'pi pi-times-circle text-red-600',
  NeedsConfirmation: 'pi pi-question-circle text-amber-600',
  Denied: 'pi pi-ban text-muted-color',
  Expired: 'pi pi-clock text-muted-color',
}

/** A waiting action past its time shows as expired before the server says so. */
function statusOf(action: Action): ActionStatus {
  return action.status === 'NeedsConfirmation' && action.confirmBy && Date.parse(action.confirmBy) < Date.now()
    ? 'Expired'
    : action.status
}

async function decide(action: Action, approve: boolean) {
  busy.value = action.id
  try {
    emit('decided', await jarvisApi.decide(action.id, approve))
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = null
  }
}
</script>

<template>
  <ul
    class="mt-2 flex flex-col gap-2 rounded-lg border border-surface-200 p-3 text-sm dark:border-surface-800"
    :aria-label="t('jarvis.actions')"
  >
    <li v-for="action in props.actions" :key="action.id" class="flex flex-col gap-2">
      <div class="flex items-start gap-2">
        <i :class="icons[statusOf(action)]" class="mt-0.5" aria-hidden="true" />
        <span class="flex-1 break-words">{{ action.description }}</span>
        <span class="shrink-0 text-xs text-muted-color">{{ t(`jarvis.status.${statusOf(action)}`) }}</span>
      </div>
      <div v-if="statusOf(action) === 'NeedsConfirmation' && !props.readonly" class="flex items-center gap-2 pl-6">
        <Button :label="t('jarvis.confirm')" size="small" :loading="busy === action.id" @click="decide(action, true)" />
        <Button
          :label="t('jarvis.cancel')"
          size="small"
          severity="secondary"
          :disabled="busy === action.id"
          @click="decide(action, false)"
        />
        <span v-if="action.confirmBy" class="text-xs text-muted-color">{{
          t('jarvis.until', { time: d(new Date(action.confirmBy), 'time') })
        }}</span>
      </div>
      <p v-else-if="statusOf(action) === 'Expired'" class="pl-6 text-xs text-muted-color">{{ t('jarvis.expired') }}</p>
    </li>
  </ul>
</template>
