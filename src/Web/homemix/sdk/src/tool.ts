// The ctx of a tool script (tools/{name}.js), which the HomeMix server runs for JARVIS in Jint (docs/homemix.md,
// "JARVIS"): the bridge's contract without the screen, so the same code can read data in the frame and in a tool.
import type { HomeMix } from './client.js'
import type { HomeMixContext, Member } from './protocol.js'

export interface ToolResponse {
  ok: boolean
  status: number
  headers: { 'content-type': string | null }
  text(): Promise<string>
  json<T = unknown>(): Promise<T>
}

export interface ToolContext {
  /** As in the frame, with household and user; no theme, page or widget. */
  context(): Promise<HomeMixContext>
  can(permission: string): Promise<boolean>
  /** put and delete fail with homemix.read_only_tool in a readOnly tool. */
  data: HomeMix['data']
  members(): Promise<Member[]>
  /** Only to the add-on's network origins, GET or POST, no redirects, at most 1 MB and 5 per run. */
  fetch(url: string, init?: { method?: 'GET' | 'POST'; body?: string; headers?: { 'content-type'?: string } }): Promise<ToolResponse>
}

/** What tools/{name}.js exports as default: its answer is a string, or anything JSON. */
export type ToolScript<A = Record<string, unknown>> = (args: A, ctx: ToolContext) => unknown
