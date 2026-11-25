<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { useCurrentHousehold } from '@/features/households/useCurrentHousehold'
import ActionCard from './ActionCard.vue'
import { AnswerError, jarvisApi, jarvisKeys, type Action, type ChatMessage } from './api'

// JARVIS (jarvis-v1.md 2.4): my conversations on the left, the open one on the right, answers as they come.
const props = defineProps<{ conversationId?: string }>()

const { t, d } = useI18n()
const toast = useToast()
const router = useRouter()
const queryClient = useQueryClient()
const { current: household } = useCurrentHousehold()

// 2.14: the list can be searched by a phrase in the title or the messages (the server matches, at most 100 characters).
const search = ref('')
const phrase = computed(() => search.value.trim().slice(0, 100))
const { data: conversations, isPending: listPending } = useQuery({
  queryKey: computed(() => (phrase.value ? jarvisKeys.search(phrase.value) : jarvisKeys.conversations)),
  queryFn: () => jarvisApi.conversations(phrase.value || undefined),
})
const openId = computed(() => props.conversationId ?? null)
const { data: messages, isPending: messagesPending } = useQuery({
  queryKey: computed(() => jarvisKeys.messages(openId.value ?? '')),
  queryFn: () => jarvisApi.messages(openId.value!),
  enabled: computed(() => !!openId.value),
})

const draft = ref('')
const sending = ref(false)
/** The answer being written: my message, JARVIS's text so far and the card so far. */
const live = ref<{ question: string; text: string; actions: Action[] } | null>(null)
const thread = ref<HTMLElement | null>(null)

function fail(error: unknown) {
  const summary =
    error instanceof AnswerError
      ? errorMessage({ status: 0, code: error.code, errors: [], fieldErrors: {} })
      : errorMessage(toProblem(error))
  toast.add({ severity: 'error', summary, life: 6000 })
}

async function scrollDown() {
  await nextTick()
  thread.value?.scrollTo({ top: thread.value.scrollHeight })
}

watch(
  () => [messages.value?.length, live.value?.text],
  () => void scrollDown(),
)

async function startNew() {
  try {
    const conversation = await jarvisApi.start()
    await queryClient.invalidateQueries({ queryKey: jarvisKeys.conversations })
    await router.push({ name: 'jarvis', params: { conversationId: conversation.id } })
  } catch (error) {
    fail(error)
  }
}

async function send() {
  const text = draft.value.trim()
  if (!text || sending.value) return
  sending.value = true
  try {
    const id = openId.value ?? (await jarvisApi.start()).id
    if (!openId.value) await router.push({ name: 'jarvis', params: { conversationId: id } })
    draft.value = ''
    live.value = { question: text, text: '', actions: [] }
    await jarvisApi.send(id, text, household.value?.id ?? null, {
      text: (piece) => {
        if (live.value) live.value.text += piece
      },
      action: (action) => live.value?.actions.push(action),
    })
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: jarvisKeys.messages(id) }),
      queryClient.invalidateQueries({ queryKey: jarvisKeys.conversations }),
    ])
  } catch (error) {
    draft.value ||= text
    fail(error)
  } finally {
    live.value = null
    sending.value = false
  }
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
    event.preventDefault()
    void send()
  }
}

async function decided(conversationId: string, action: Action) {
  queryClient.setQueryData<ChatMessage[]>(jarvisKeys.messages(conversationId), (list) =>
    list?.map((m) => ({ ...m, actions: m.actions.map((a) => (a.id === action.id ? action : a)) })),
  )
}

async function remove() {
  if (!openId.value) return
  try {
    await jarvisApi.remove(openId.value)
    await queryClient.invalidateQueries({ queryKey: jarvisKeys.conversations })
    await router.push({ name: 'jarvis' })
  } catch (error) {
    fail(error)
  }
}

const renaming = ref(false)
const newTitle = ref('')
function startRename() {
  newTitle.value = title.value
  renaming.value = true
}
async function rename() {
  const value = newTitle.value.trim()
  if (!openId.value || !value) return
  try {
    await jarvisApi.rename(openId.value, value)
    renaming.value = false
    await queryClient.invalidateQueries({ queryKey: jarvisKeys.conversations })
  } catch (error) {
    fail(error)
  }
}

const title = computed(
  () => conversations.value?.find((c) => c.id === openId.value)?.title ?? t('jarvis.newConversation'),
)
</script>

<template>
  <div class="mx-auto flex max-w-6xl flex-col gap-4 md:h-[calc(100dvh-8rem)] md:flex-row">
    <aside class="flex shrink-0 flex-col gap-2 md:w-64" :aria-label="t('jarvis.conversations')">
      <Button :label="t('jarvis.newConversation')" icon="pi pi-plus" size="small" @click="startNew" />
      <InputText
        v-model="search"
        type="search"
        size="small"
        maxlength="100"
        :placeholder="t('jarvis.search')"
        :aria-label="t('jarvis.search')"
        data-testid="jarvis-search"
      />
      <div v-if="listPending" class="flex flex-col gap-2">
        <Skeleton v-for="i in 3" :key="i" height="2.5rem" />
      </div>
      <nav v-else class="flex max-h-48 flex-col gap-1 overflow-y-auto md:max-h-none">
        <RouterLink
          v-for="conversation in conversations"
          :key="conversation.id"
          :to="{ name: 'jarvis', params: { conversationId: conversation.id } }"
          class="truncate rounded-md px-3 py-2 text-sm hover:bg-emphasis"
          :class="conversation.id === openId ? 'bg-emphasis font-medium' : 'text-muted-color'"
        >
          {{ conversation.title }}
        </RouterLink>
        <p v-if="conversations?.length === 0" class="px-3 text-sm text-muted-color">
          {{ phrase ? t('jarvis.noMatches') : t('jarvis.noConversations') }}
        </p>
      </nav>
    </aside>

    <section class="flex min-h-[60dvh] flex-1 flex-col rounded-lg border border-surface-200 dark:border-surface-800">
      <header class="flex items-center gap-2 border-b border-surface-200 px-4 py-2 dark:border-surface-800">
        <i class="pi pi-sparkles text-primary" aria-hidden="true" />
        <form v-if="renaming" class="flex flex-1 items-center gap-1" @submit.prevent="rename">
          <InputText
            v-model="newTitle"
            size="small"
            maxlength="80"
            class="flex-1"
            :aria-label="t('jarvis.renameTitle')"
            data-testid="jarvis-rename-input"
          />
          <Button type="submit" icon="pi pi-check" size="small" text rounded :aria-label="t('common.save')" :disabled="!newTitle.trim()" />
          <Button icon="pi pi-times" size="small" text rounded severity="secondary" :aria-label="t('common.cancel')" @click="renaming = false" />
        </form>
        <h1 v-else class="flex-1 truncate font-semibold">{{ openId ? title : t('jarvis.title') }}</h1>
        <span v-if="household" class="hidden text-xs text-muted-color sm:inline">{{
          t('jarvis.household', { name: household.name })
        }}</span>
        <Button
          v-if="openId && !renaming"
          icon="pi pi-pencil"
          severity="secondary"
          text
          rounded
          :aria-label="t('jarvis.rename')"
          data-testid="jarvis-rename"
          @click="startRename"
        />
        <Button
          v-if="openId"
          icon="pi pi-trash"
          severity="secondary"
          text
          rounded
          :aria-label="t('jarvis.delete')"
          @click="remove"
        />
      </header>

      <div ref="thread" class="flex flex-1 flex-col gap-4 overflow-y-auto p-4" aria-live="polite">
        <Message v-if="!openId && !live" severity="secondary" :closable="false">{{ t('jarvis.intro') }}</Message>
        <Skeleton v-else-if="openId && messagesPending && !live" height="4rem" />
        <template v-for="message in messages ?? []" :key="message.id">
          <div
            v-if="message.role === 'Person'"
            class="ml-auto max-w-[85%] rounded-lg bg-primary px-3 py-2 whitespace-pre-wrap text-primary-contrast"
          >
            {{ message.text }}
          </div>
          <div v-else class="max-w-[85%]">
            <p class="whitespace-pre-wrap">{{ message.text }}</p>
            <ActionCard
              v-if="message.actions.length"
              :actions="message.actions"
              @decided="(a) => decided(openId!, a)"
            />
            <p class="mt-1 text-xs text-muted-color">{{ d(new Date(message.sentAt), 'long') }}</p>
          </div>
        </template>
        <template v-if="live">
          <div class="ml-auto max-w-[85%] rounded-lg bg-primary px-3 py-2 whitespace-pre-wrap text-primary-contrast">
            {{ live.question }}
          </div>
          <div class="max-w-[85%]">
            <p v-if="live.text" class="whitespace-pre-wrap">{{ live.text }}</p>
            <p v-else class="text-muted-color">
              <i class="pi pi-spin pi-spinner mr-2" aria-hidden="true" />{{ t('jarvis.thinking') }}
            </p>
            <ActionCard v-if="live.actions.length" :actions="live.actions" readonly />
          </div>
        </template>
      </div>

      <form class="flex items-end gap-2 border-t border-surface-200 p-3 dark:border-surface-800" @submit.prevent="send">
        <Textarea
          v-model="draft"
          auto-resize
          rows="1"
          class="max-h-40 flex-1"
          :placeholder="t('jarvis.placeholder')"
          :aria-label="t('jarvis.placeholder')"
          :maxlength="4000"
          @keydown="onKeydown"
        />
        <Button
          type="submit"
          icon="pi pi-send"
          :loading="sending"
          :disabled="!draft.trim()"
          :aria-label="t('jarvis.send')"
        />
      </form>
    </section>
  </div>
</template>
