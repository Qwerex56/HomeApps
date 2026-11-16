import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { homemixApi, mixKeys } from '../api'

/** What the author (or the Owner) does with a store entry: read it again from GitHub now, or remove it. */
export function useListingActions() {
  const { t } = useI18n()
  const toast = useToast()
  const confirm = useConfirm()
  const queryClient = useQueryClient()
  const busy = ref<string | null>(null)

  async function run(key: string, action: () => Promise<unknown>, done: string) {
    busy.value = key
    try {
      await action()
      toast.add({ severity: 'success', summary: done, life: 3000 })
      await queryClient.invalidateQueries({ queryKey: mixKeys.all })
      return true
    } catch (error) {
      toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
      return false
    } finally {
      busy.value = null
    }
  }

  return {
    busy,
    refresh: (key: string) => run(key, () => homemixApi.store.refresh(key), t('homemix.store.refreshed')),
    /** Asks first; `then` runs after the entry is gone (e.g. leaving its page). */
    remove: (key: string, name: string, then?: () => unknown) =>
      confirm.require({
        header: t('homemix.store.remove'),
        message: t('homemix.store.removeConfirm', { name }),
        acceptProps: { label: t('homemix.store.remove'), severity: 'danger' },
        rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
        accept: async () => {
          if (await run(key, () => homemixApi.store.remove(key), t('homemix.store.removed'))) await then?.()
        },
      }),
  }
}
