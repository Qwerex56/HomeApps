<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import { localized } from '@/features/plugins/registry'
import { hostOf, type Package } from './api'

// The consent screen (docs/homemix.md, "Installing"): what the add-on may do and where it may send data.
defineProps<{ pkg: Package; notSupported?: boolean }>()
const jarvis = defineModel<boolean>('jarvis', { default: false })
const { t } = useI18n()
</script>

<template>
  <div class="flex flex-col gap-4">
    <div class="flex items-start gap-3">
      <i :class="['pi', pkg.icon, 'text-3xl text-primary']" aria-hidden="true" />
      <div class="min-w-0">
        <div class="text-lg font-semibold">
          {{ localized(pkg.name) }} <span class="text-muted-color">{{ pkg.version }}</span>
        </div>
        <p v-if="pkg.description" class="text-muted-color">{{ localized(pkg.description) }}</p>
        <a
          v-if="pkg.repository"
          :href="pkg.repository"
          target="_blank"
          rel="noopener noreferrer"
          class="text-sm text-primary hover:underline"
        >
          {{ pkg.repository }}
        </a>
        <p class="font-mono text-xs break-all text-muted-color">SHA-256 {{ pkg.sha256 }}</p>
      </div>
    </div>
    <Message v-if="notSupported" severity="warn" :closable="false">{{ t('homemix.targetNotSupported') }}</Message>
    <section>
      <h3 class="mb-1 font-medium">{{ t('homemix.consent.may') }}</h3>
      <ul class="list-inside list-disc text-sm">
        <li>{{ t('homemix.consent.always') }}</li>
        <li v-for="c in pkg.capabilities" :key="c">{{ t(`homemix.capability.${c}`) }}</li>
      </ul>
    </section>
    <section v-if="pkg.network.length">
      <h3 class="mb-1 font-medium">{{ t('homemix.consent.network') }}</h3>
      <ul class="list-inside list-disc text-sm">
        <li v-for="origin in pkg.network" :key="origin" class="font-mono">{{ hostOf(origin) }}</li>
      </ul>
    </section>
    <label v-if="pkg.jarvis" class="flex items-center gap-2 text-sm">
      <Checkbox v-model="jarvis" binary input-id="homemix-jarvis" />
      {{ t('homemix.consent.jarvis') }}
    </label>
    <p class="text-sm text-muted-color">{{ t('homemix.consent.never') }}</p>
  </div>
</template>
