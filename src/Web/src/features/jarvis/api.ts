import { http } from '@/api/http'
import type { components } from '@/api/types/jarvis'
import { useSessionStore } from '@/features/auth/session'

// JARVIS in the app (docs/jarvis.md): the person's own conversations, answered live over server-sent events.

type Schemas = components['schemas']
export type Conversation = Schemas['ConversationView']
export type ChatMessage = Schemas['MessageView']
export type Action = Schemas['ActionView']
export type ActionStatus = Schemas['ToolCallStatus']

export const jarvisKeys = {
  conversations: ['jarvis', 'conversations'] as const,
  messages: (id: string) => ['jarvis', 'conversations', id, 'messages'] as const,
}

/** What arrives while JARVIS answers: pieces of text and card lines, then the kept message. */
export interface AnswerHandlers {
  text: (piece: string) => void
  action: (action: Action) => void
}

/** An error event of the stream, shaped like a problem's code for errorMessage(). */
export class AnswerError extends Error {
  constructor(public readonly code: string) {
    super(code)
  }
}

export const jarvisApi = {
  async conversations() {
    return (await http.get<Conversation[]>('/jarvis/api/conversations')).data
  },

  async start() {
    return (await http.post<Conversation>('/jarvis/api/conversations', { title: null })).data
  },

  async remove(id: string) {
    await http.delete(`/jarvis/api/conversations/${id}`)
  },

  async messages(id: string) {
    return (await http.get<ChatMessage[]>(`/jarvis/api/conversations/${id}/messages`)).data
  },

  async decide(actionId: string, approve: boolean) {
    return (await http.post<Action>(`/jarvis/api/confirmations/${actionId}`, { approve })).data
  },

  /**
   * Sends a message and reads the answer as it comes. fetch, not EventSource: the request is a POST with a body and
   * the access token, and an expired token is refreshed once like in http.ts.
   */
  async send(id: string, text: string, householdId: string | null, on: AnswerHandlers, signal?: AbortSignal) {
    const session = useSessionStore()
    const post = (token: string | null) =>
      fetch(`/jarvis/api/conversations/${id}/messages`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'text/event-stream',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ text, householdId }),
        signal,
      })

    let response = await post(session.accessToken)
    if (response.status === 401) {
      response = await post(await session.refresh())
    }
    if (!response.ok || !response.body) {
      throw new AnswerError(response.status === 0 ? 'network' : 'unknown')
    }

    let message: ChatMessage | null = null
    for await (const event of readEvents(response.body)) {
      if (event.event === 'text') on.text(event.data as string)
      else if (event.event === 'action') on.action(event.data as Action)
      else if (event.event === 'message') message = event.data as ChatMessage
      else if (event.event === 'error') throw new AnswerError((event.data as { code?: string }).code ?? 'unknown')
    }
    if (!message) {
      throw new AnswerError('network')
    }
    return message
  },
}

export interface StreamEvent {
  event: string
  data: unknown
}

/** Server-sent events from a body: blocks split by a blank line, data as JSON (a text piece is a JSON string). */
export async function* readEvents(body: ReadableStream<Uint8Array>): AsyncGenerator<StreamEvent> {
  const reader = body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  for (;;) {
    const { value, done } = await reader.read()
    buffer = (buffer + decoder.decode(value, { stream: !done })).replace(/\r\n/g, '\n')
    let end: number
    while ((end = buffer.indexOf('\n\n')) >= 0) {
      const parsed = parseBlock(buffer.slice(0, end))
      buffer = buffer.slice(end + 2)
      if (parsed) yield parsed
    }
    if (done) {
      const parsed = parseBlock(buffer)
      if (parsed) yield parsed
      return
    }
  }
}

function parseBlock(block: string): StreamEvent | null {
  let event = 'message'
  const data: string[] = []
  for (const line of block.split('\n')) {
    if (line.startsWith('event:')) event = line.slice(6).trim()
    else if (line.startsWith('data:')) data.push(line.slice(5).replace(/^ /, ''))
  }
  if (data.length === 0) return null
  const raw = data.join('\n')
  try {
    return { event, data: JSON.parse(raw) }
  } catch {
    return { event, data: raw }
  }
}
