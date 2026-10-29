import { computed, type MaybeRefOrGetter, toValue } from 'vue'
import { useToast } from 'primevue/usetoast'
import { hasPermission, type HostApi, type PluginHousehold, type PluginManifest } from '@homeapps/plugin-sdk'
import { http } from '@/api/http'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { useSessionStore } from '@/features/auth/session'
import { i18n } from '@/shared/i18n'
import { APP_VERSION } from './registry'

/** The HostApi for one plugin in one place. Call it in a component's setup (it uses the toast and the session). */
export function createHost(manifest: PluginManifest, household: MaybeRefOrGetter<PluginHousehold | null>): HostApi {
  const toast = useToast()
  const session = useSessionStore()
  const current = computed(() => toValue(household))

  return {
    version: APP_VERSION,
    manifest,
    http,
    apiBase: manifest.endpoints.api,
    household: current,
    user: computed(() => (session.claims ? { id: session.claims.sub, email: session.claims.email ?? null } : null)),
    can: (permission) => hasPermission(manifest, current.value?.myRole, permission),
    t: (key, values) => i18n.global.t(`plugins.${manifest.id}.${key}`, values ?? {}),
    notify: (severity, summary) => toast.add({ severity, summary, life: severity === 'error' ? 6000 : 3000 }),
    errorMessage: (error) => errorMessage(toProblem(error)),
  }
}
