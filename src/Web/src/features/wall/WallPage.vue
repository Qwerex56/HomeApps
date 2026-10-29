<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { householdKeys, householdPermissions, householdsApi } from '@/features/households/api'
import { useCurrentHousehold } from '@/features/households/useCurrentHousehold'
import { useWakeLock } from './useWakeLock'
import PluginWidgets from '@/features/plugins/PluginWidgets.vue'
import { useHouseholdPlugins } from '@/features/plugins/registry'

// Q7, Q11: the wall tablet. Full screen, big type, no menu; the screen stays on. Plugins add tiles later.
const { t } = useI18n()
const { current, isPending } = useCurrentHousehold()
const awake = useWakeLock()

const now = ref(new Date())
let tick: ReturnType<typeof setInterval> | undefined
onMounted(() => (tick = setInterval(() => (now.value = new Date()), 1000)))
onBeforeUnmount(() => clearInterval(tick))

const time = computed(() => now.value.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' }))
const date = computed(() => now.value.toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long' }))

// The members are read again every few minutes, so a new person or a name change shows up without a reload.
const { data: members } = useQuery({
  queryKey: computed(() => householdKeys.members(current.value?.id ?? '')),
  queryFn: () => householdsApi.members(current.value!.id),
  enabled: computed(() => householdPermissions(current.value?.myRole).seeMembers),
  refetchInterval: 5 * 60_000,
})
const people = computed(() => (members.value ?? []).filter((m) => m.isActive))

// The widgets of the plugins that are on in this household and may stand on the wall (docs/plugins.md). Until
// there is one, the slots show what is coming.
const { enabled } = useHouseholdPlugins(() => current.value?.id)
const hasWidgets = computed(() =>
  enabled.value.some((p) => p.manifest.ui?.widgets.some((w) => w.places.includes('Wall') && p.module?.widgets?.[w.id])),
)
const tiles = [
  { key: 'weather', icon: 'pi pi-cloud' },
  { key: 'news', icon: 'pi pi-megaphone' },
]

const fullscreen = ref(false)
async function toggleFullscreen() {
  if (document.fullscreenElement) await document.exitFullscreen()
  else await document.documentElement.requestFullscreen?.().catch(() => undefined)
}
const onFullscreen = () => (fullscreen.value = !!document.fullscreenElement)
onMounted(() => document.addEventListener('fullscreenchange', onFullscreen))
onBeforeUnmount(() => document.removeEventListener('fullscreenchange', onFullscreen))
</script>

<template>
  <div class="flex min-h-dvh flex-col gap-8 bg-surface-950 p-8 text-surface-0 select-none lg:p-12">
    <header class="flex items-start justify-between gap-6">
      <div>
        <time class="block text-8xl leading-none font-light tabular-nums lg:text-9xl" :datetime="now.toISOString()">
          {{ time }}
        </time>
        <div class="mt-3 text-3xl font-light text-surface-300 first-letter:uppercase">{{ date }}</div>
      </div>
      <div class="flex gap-2 opacity-40 transition hover:opacity-100">
        <button
          type="button"
          class="rounded-full p-3 hover:bg-surface-800"
          :aria-label="fullscreen ? t('wall.exitFullscreen') : t('wall.fullscreen')"
          @click="toggleFullscreen"
        >
          <i
            :class="fullscreen ? 'pi pi-window-minimize' : 'pi pi-window-maximize'"
            class="text-xl"
            aria-hidden="true"
          />
        </button>
        <RouterLink :to="{ name: 'home' }" class="rounded-full p-3 hover:bg-surface-800" :aria-label="t('wall.exit')">
          <i class="pi pi-times text-xl" aria-hidden="true" />
        </RouterLink>
      </div>
    </header>

    <main class="grid flex-1 content-start gap-6 lg:grid-cols-3">
      <section class="rounded-3xl bg-surface-900 p-8 lg:col-span-2">
        <template v-if="current">
          <h1 class="text-4xl font-semibold">{{ current.name }}</h1>
          <ul v-if="people.length" class="mt-8 flex flex-wrap gap-8">
            <li v-for="member in people" :key="member.userId" class="flex w-28 flex-col items-center gap-3 text-center">
              <img v-if="member.avatarUrl" :src="member.avatarUrl" alt="" class="size-24 rounded-full object-cover" />
              <span
                v-else
                class="flex size-24 items-center justify-center rounded-full bg-primary text-4xl font-semibold text-primary-contrast"
                aria-hidden="true"
                >{{ member.name.slice(0, 1).toUpperCase() }}</span
              >
              <span class="w-full truncate text-xl">{{ member.name }}</span>
            </li>
          </ul>
        </template>
        <p v-else-if="!isPending" class="text-2xl text-surface-300">{{ t('wall.noHousehold') }}</p>
      </section>

      <PluginWidgets
        v-if="current && hasWidgets"
        class="lg:col-span-3"
        :household="current"
        place="Wall"
        dark
      />
      <section v-else class="grid content-start gap-6">
        <div
          v-for="tile in tiles"
          :key="tile.key"
          class="flex items-center gap-5 rounded-3xl border-2 border-dashed border-surface-700 p-8 text-surface-400"
        >
          <i :class="tile.icon" class="text-4xl" aria-hidden="true" />
          <div>
            <div class="text-2xl">{{ t(`wall.tiles.${tile.key}`) }}</div>
            <div class="text-surface-500">{{ t('wall.soon') }}</div>
          </div>
        </div>
      </section>
    </main>

    <footer v-if="!awake" class="text-sm text-surface-500">{{ t('wall.noWakeLock') }}</footer>
  </div>
</template>
