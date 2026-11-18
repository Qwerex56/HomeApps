<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { formatPairingCode, type PairingCode } from '../api'

// The code is shown once and lives 10 minutes; the satellite (or the Mac bridge) pairs with it.
const props = defineProps<{ code: PairingCode | null }>()
const emit = defineEmits<{ close: [] }>()
const { t, d } = useI18n()
const shown = computed(() => (props.code ? formatPairingCode(props.code.code) : ''))
</script>

<template>
  <Dialog
    :visible="!!code"
    modal
    :header="t('devices.codeTitle', { name: code?.name })"
    class="w-full max-w-md"
    @update:visible="(visible: boolean) => !visible && emit('close')"
  >
    <div v-if="code" class="flex flex-col gap-4">
      <p
        class="rounded-md bg-emphasis py-4 text-center font-mono text-3xl tracking-widest select-all"
        data-testid="pairing-code"
      >
        {{ shown }}
      </p>
      <p class="text-sm text-muted-color">
        {{ t('devices.codeExpires', { when: d(new Date(code.expiresAt), 'long') }) }}
      </p>
      <Message severity="info" :closable="false">
        <div class="flex flex-col gap-2">
          <span>{{ t('devices.codeHow') }}</span>
          <code class="text-sm">--pair {{ shown }}</code>
        </div>
      </Message>
    </div>
    <template #footer>
      <Button :label="t('devices.done')" @click="emit('close')" />
    </template>
  </Dialog>
</template>
