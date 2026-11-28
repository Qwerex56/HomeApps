import { http } from '@/api/http'

// The model connection of JARVIS (D14). The key goes in and never comes out: the server says only whether one is set.
export interface LlmSettings {
  baseUrl: string
  model: string
  apiKeySet: boolean
  timeoutSeconds: number
}

/** `apiKey`: absent keeps the stored key, an empty string removes it. */
export interface LlmSettingsInput {
  baseUrl: string
  model: string
  apiKey?: string
  timeoutSeconds: number
}

export interface LlmCheck {
  ok: boolean
  error: string | null
}

export const llmSettingsKeys = { settings: ['admin', 'jarvis', 'llm-settings'] as const }

export const llmSettingsApi = {
  async get() {
    return (await http.get<LlmSettings>('/jarvis/api/v1/admin/llm-settings')).data
  },

  async save(input: LlmSettingsInput) {
    return (await http.put<LlmSettings>('/jarvis/api/v1/admin/llm-settings', input)).data
  },

  async test() {
    return (await http.post<LlmCheck>('/jarvis/api/v1/admin/llm-settings/test')).data
  },
}
