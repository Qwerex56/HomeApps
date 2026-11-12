<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import SelectButton from 'primevue/selectbutton'
import { useSubmit } from '@/shared/composables/useSubmit'
import { fullConsent, homemixApi, type Installation, type MixTarget, type Package, type StoreEntry } from './api'
import ConsentPanel from './ConsentPanel.vue'
import StoreList from './StoreList.vue'

// Adding an add-on: from the store, a GitHub repository or a zip, then the consent screen, then installing (docs/homemix.md).
// With `update`, the package is a new version of that installation that asked for more, so it needs consent again.
const props = defineProps<{ target: MixTarget; update?: { installation: Installation; pkg: Package } | null }>()
const emit = defineEmits<{ done: [installation: Installation] }>()
const visible = defineModel<boolean>('visible', { required: true })
const { t } = useI18n()
const { busy, formError, submit } = useSubmit()

const source = ref<'store' | 'github' | 'zip'>('store')
const repository = ref('')
const file = ref<File | null>(null)
const pkg = ref<Package | null>(null)
const jarvis = ref(false)

watch(visible, (open) => {
  if (open) {
    pkg.value = props.update?.pkg ?? null
    jarvis.value = props.update?.installation.jarvisAllowed ?? false
    repository.value = ''
    file.value = null
    formError.value = null
  }
})

const sources = computed(() => [
  { label: t('homemix.store.title'), value: 'store' },
  { label: t('homemix.add.github'), value: 'github' },
  { label: t('homemix.add.zip'), value: 'zip' },
])
const notSupported = computed(() => !!pkg.value && !pkg.value.targets.includes(props.target.kind))

function pick(event: Event) {
  file.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

async function read() {
  await submit(async () => {
    pkg.value =
      source.value === 'github'
        ? await homemixApi.addGitHub(repository.value.trim())
        : await homemixApi.addZip(file.value!)
  })
}

async function choose(entry: StoreEntry) {
  await submit(async () => {
    pkg.value = entry.official
      ? await homemixApi.addOfficial(entry.addonId!)
      : await homemixApi.addGitHub(entry.repository!)
  })
}

async function install() {
  const chosen = pkg.value!
  await submit(async () => {
    const consent = fullConsent(chosen, jarvis.value)
    const installation = props.update
      ? await homemixApi.update(props.update.installation.id, chosen.sha256, consent)
      : await homemixApi.install(props.target, chosen.sha256, consent)
    emit('done', installation)
    visible.value = false
  })
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    modal
    :header="update ? t('homemix.update.title') : t('homemix.add.title')"
    class="w-full max-w-xl"
  >
    <div class="flex flex-col gap-4">
      <template v-if="!pkg">
        <SelectButton
          v-model="source"
          :options="sources"
          option-label="label"
          option-value="value"
          :allow-empty="false"
        />
        <StoreList v-if="source === 'store'" :busy="busy" @choose="choose" />
        <InputText
          v-else-if="source === 'github'"
          v-model="repository"
          placeholder="https://github.com/owner/repo"
          :aria-label="t('homemix.add.github')"
        />
        <input v-else type="file" accept=".zip,application/zip" :aria-label="t('homemix.add.zip')" @change="pick" />
        <p v-if="source !== 'store'" class="text-sm text-muted-color">{{ t('homemix.add.hint') }}</p>
      </template>
      <ConsentPanel v-else-if="pkg" v-model:jarvis="jarvis" :pkg="pkg" :not-supported="notSupported" />
      <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
    </div>
    <template #footer>
      <Button :label="t('common.cancel')" severity="secondary" text @click="visible = false" />
      <Button
        v-if="!pkg && source !== 'store'"
        :label="t('homemix.add.read')"
        :loading="busy"
        :disabled="source === 'github' ? !repository.trim() : !file"
        @click="read"
      />
      <Button
        v-else-if="pkg"
        :label="update ? t('homemix.update.accept') : t('homemix.add.install')"
        :loading="busy"
        :disabled="notSupported"
        @click="install"
      />
    </template>
  </Dialog>
</template>
