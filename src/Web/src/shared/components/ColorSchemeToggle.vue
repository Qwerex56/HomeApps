<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import { useColorScheme, type ColorScheme } from '@/shared/composables/useColorScheme'

const { t } = useI18n()
const { scheme, setScheme } = useColorScheme()

const order: ColorScheme[] = ['system', 'light', 'dark']
const icons: Record<ColorScheme, string> = { system: 'pi pi-desktop', light: 'pi pi-sun', dark: 'pi pi-moon' }

const label = computed(() => `${t('colorScheme.label')}: ${t(`colorScheme.${scheme.value}`)}`)

function next() {
  setScheme(order[(order.indexOf(scheme.value) + 1) % order.length]!)
}
</script>

<template>
  <Button :icon="icons[scheme]" :aria-label="label" v-tooltip.bottom="label" text rounded @click="next" />
</template>
