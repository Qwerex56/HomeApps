<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import UserAvatar from '@/shared/components/UserAvatar.vue'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { accountApi, AVATAR_MAX_BYTES, AVATAR_TYPES } from '../api'

// 2.15: pick a picture, check it here (2 MB, JPG/PNG/WebP), send it; the server checks it again.
defineProps<{ name: string; avatarUrl: string | null }>()
const emit = defineEmits<{ uploaded: [] }>()
const { t } = useI18n()
const toast = useToast()
const input = ref<HTMLInputElement>()
const busy = ref(false)

async function choose(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  target.value = ''
  if (!file) return
  if (!AVATAR_TYPES.includes(file.type)) {
    toast.add({ severity: 'error', summary: t('account.profile.avatar.badType'), life: 6000 })
    return
  }
  if (file.size > AVATAR_MAX_BYTES) {
    toast.add({ severity: 'error', summary: t('account.profile.avatar.tooLarge'), life: 6000 })
    return
  }
  busy.value = true
  try {
    await accountApi.uploadAvatar(file)
    emit('uploaded')
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div class="flex items-center gap-4">
    <UserAvatar :src="avatarUrl" :name="name" size="xlarge" />
    <div class="flex flex-col gap-1">
      <Button
        :label="t('account.profile.avatar.choose')"
        icon="pi pi-upload"
        severity="secondary"
        outlined
        size="small"
        :loading="busy"
        data-testid="avatar-choose"
        @click="input?.click()"
      />
      <span class="text-sm text-muted-color">{{ t('account.profile.avatar.hint') }}</span>
      <input ref="input" type="file" accept="image/jpeg,image/png,image/webp" class="hidden" data-testid="avatar-input" @change="choose" />
    </div>
  </div>
</template>
