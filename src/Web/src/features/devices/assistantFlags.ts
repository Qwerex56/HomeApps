// Households serializes AssistantToolSources ([Flags] enum) as a string: "None", "Plugins, HomeMix", or "All".

export type AssistantToolSource = 'Plugins' | 'HomeMix' | 'Devices'

export const ALL_TOOL_SOURCES: readonly AssistantToolSource[] = ['Plugins', 'HomeMix', 'Devices']

export function parseToolSources(raw: string | null | undefined): AssistantToolSource[] {
  if (!raw) return []
  const tokens = raw
    .split(',')
    .map((part) => part.trim())
    .filter(Boolean)
  if (tokens.includes('All')) return [...ALL_TOOL_SOURCES]
  if (tokens.includes('None')) return []
  return ALL_TOOL_SOURCES.filter((source) => tokens.includes(source))
}

export function serializeToolSources(sources: Iterable<AssistantToolSource>): string {
  const picked = new Set(sources)
  const ordered = ALL_TOOL_SOURCES.filter((source) => picked.has(source))
  if (ordered.length === 0) return 'None'
  if (ordered.length === ALL_TOOL_SOURCES.length) return 'All'
  return ordered.join(', ')
}
