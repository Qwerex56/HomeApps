// homemix tool: runs a tool script (tools/{name}.js) as the HomeMix server would for JARVIS, with a ctx over data in
// memory, the add-on's network origins and the server's limits. node:vm is not a security boundary: this mirrors the
// server's contract and limits, not its isolation (the server runs scripts in Jint).
import { readFile } from 'node:fs/promises'
import { join } from 'node:path'
import vm from 'node:vm'
import { readManifest } from './files.js'

export const LIMITS = { timeMs: 5000, fetchMs: 3000, fetchBytes: 1024 * 1024, fetches: 5, hostCalls: 100 }
const roles = ['Guest', 'Member', 'Admin', 'Owner']

class ToolError extends Error {
  constructor(code, message) {
    super(message)
    this.code = code
  }
}

/** The tool's answer as JARVIS would get it, and the data afterwards. */
export async function runTool(dir, name, args = {}, { data = {}, role = 'Admin' } = {}) {
  const manifest = await readManifest(dir)
  const tool = (manifest.tools ?? []).find((t) => t.name === name && t.action === 'script')
  if (!tool) throw new Error(`homemix.json has no script tool '${name}'.`)
  const source = await readFile(join(dir, 'tools', `${name}.js`), 'utf8')

  const input = { ...args }
  for (const [field, spec] of Object.entries(tool.fields ?? {})) {
    if (input[field] === undefined && spec.default !== undefined) input[field] = spec.default
    else if (input[field] === undefined && spec.required) throw new Error(`${field} is required.`)
  }
  for (const key of Object.keys(input))
    if (!(key in (tool.fields ?? {}))) throw new Error(`Unknown argument ${key}.`)

  const store = structuredClone(data)
  const ctx = createContext(manifest, tool, store, role)
  // No require, process, fetch or URL: only what Jint has, plus console for debugging.
  const sandbox = vm.createContext({ console })
  const code = source
    .replace(/^export\s+default\s+/m, 'globalThis.__tool = ')
    .replace(/^export\s+(?=(const|let|function|async|class)\b)/gm, '')
  vm.runInContext(code, sandbox, { timeout: LIMITS.timeMs, filename: `tools/${name}.js` })
  if (typeof sandbox.__tool !== 'function') throw new Error(`tools/${name}.js must export a default function (args, ctx).`)

  let timer
  const limit = new Promise((_, reject) => {
    timer = setTimeout(() => reject(new Error(`it took longer than ${LIMITS.timeMs / 1000} s.`)), LIMITS.timeMs)
  })
  try {
    const result = await Promise.race([sandbox.__tool(input, ctx), limit])
    return { answer: typeof result === 'string' ? result : result == null ? '' : JSON.stringify(result), data: store }
  } finally {
    clearTimeout(timer)
  }
}

function createContext(manifest, tool, store, role) {
  const capabilities = manifest.capabilities ?? []
  const permissions = (manifest.permissions ?? [])
    .filter((p) => roles.indexOf(role) >= roles.indexOf(p.minRole))
    .map((p) => p.id)
  const context = Object.freeze({
    addon: { id: manifest.id, name: manifest.name.pl, version: manifest.version },
    installation: { id: 'dev-installation', target: 'Household' },
    household: { id: 'dev-household', name: 'Dom', myRole: role },
    user: { id: 'dev-user', name: 'Ty' },
    permissions,
    capabilities,
    language: 'pl',
    theme: { dark: false, vars: {} },
    page: '',
    widget: null,
  })
  let calls = 0
  let fetches = 0
  const host = (fn) => async (...a) => {
    if (++calls > LIMITS.hostCalls) throw new ToolError('homemix.script_limit', `At most ${LIMITS.hostCalls} ctx calls in one run.`)
    return fn(...a)
  }
  const space = (scope = 'household') => {
    if (scope !== 'household' && scope !== 'user') throw new ToolError('homemix.bad_request', 'scope must be household or user.')
    if (!capabilities.includes(`storage.${scope}`))
      throw new ToolError('homemix.capability_missing', `The add-on did not declare storage.${scope}.`)
    return (store[scope] ??= {})
  }
  const document = (key, entry) => ({ key, ...entry })
  const writable = () => {
    if (tool.readOnly) throw new ToolError('homemix.read_only_tool', 'This tool is readOnly in homemix.json: it may not change data.')
  }

  return Object.freeze({
    context: async () => context,
    can: async (permission) => permissions.includes(permission),
    data: Object.freeze({
      list: host((collection, scope) =>
        Object.entries(space(scope)[collection] ?? {}).map(([key, value]) => document(key, wrap(value))),
      ),
      get: host((collection, key, scope) => {
        const value = space(scope)[collection]?.[key]
        return value === undefined ? null : document(key, wrap(value))
      }),
      put: host((collection, key, value, options = {}) => {
        writable()
        const docs = (space(options.scope)[collection] ??= {})
        const current = docs[key] === undefined ? null : wrap(docs[key])
        if (options.version != null && current && current.version !== options.version)
          throw new ToolError('homemix.document_conflict', 'Someone changed it in the meantime.')
        docs[key] = { value: JSON.parse(JSON.stringify(value)), version: (current?.version ?? 0) + 1 }
        return document(key, wrap(docs[key]))
      }),
      delete: host(async (collection, key, options = {}) => {
        writable()
        delete space(options.scope)[collection]?.[key]
      }),
    }),
    members: host(() => {
      if (!capabilities.includes('household.members'))
        throw new ToolError('homemix.capability_missing', 'The add-on did not declare household.members.')
      return [{ id: 'dev-user', name: 'Ty', avatarUrl: null, role }]
    }),
    fetch: host(async (url, init = {}) => {
      const target = new URL(String(url))
      const allowed = (manifest.network ?? []).some((o) => new URL(o).origin === target.origin)
      if (target.protocol !== 'https:' || target.username || !allowed)
        throw new ToolError('homemix.network_denied', `${url} is not in the add-on's network.`)
      if (++fetches > LIMITS.fetches) throw new ToolError('homemix.script_limit', `At most ${LIMITS.fetches} fetches in one run.`)
      const method = (init.method ?? 'GET').toUpperCase()
      if (method !== 'GET' && method !== 'POST') throw new ToolError('homemix.bad_request', 'fetch takes GET or POST.')
      const response = await fetch(target, {
        method,
        body: method === 'POST' ? init.body : undefined,
        headers: init.headers?.['content-type'] ? { 'content-type': init.headers['content-type'] } : undefined,
        redirect: 'manual',
        signal: AbortSignal.timeout(LIMITS.fetchMs),
      })
      const body = await response.text()
      if (Buffer.byteLength(body) > LIMITS.fetchBytes)
        throw new ToolError('homemix.script_limit', `The answer from ${target.host} is larger than ${LIMITS.fetchBytes / 1024} KB.`)
      return Object.freeze({
        ok: response.ok,
        status: response.status,
        headers: { 'content-type': response.headers.get('content-type') },
        text: async () => body,
        json: async () => JSON.parse(body),
      })
    }),
  })
}

/** --data holds plain values; documents get a version and author like the server's. */
function wrap(entry) {
  return entry && typeof entry === 'object' && 'version' in entry && 'value' in entry
    ? { value: entry.value, version: entry.version, updatedBy: 'dev-user', updatedAt: new Date(0).toISOString() }
    : { value: entry, version: 1, updatedBy: 'dev-user', updatedAt: new Date(0).toISOString() }
}
