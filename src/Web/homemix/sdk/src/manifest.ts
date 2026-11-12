// homemix.json (docs/homemix.md, "The package") and the same checks the server makes, for `homemix check`.
import type { Capability, HouseholdRole, WidgetPlace } from './protocol.js'

export interface LocalizedText {
  pl: string
  en?: string
}

export interface HomeMixManifest {
  homemixVersion: 1
  id: string
  version: string
  requires: { homeApps: string }
  name: LocalizedText
  description?: LocalizedText
  icon?: string
  targets?: ('Household' | 'User')[]
  entry: string
  pages?: { path: string; title: LocalizedText; menu?: boolean }[]
  widgets?: {
    id: string
    title: LocalizedText
    size?: 'Small' | 'Medium' | 'Large'
    places?: WidgetPlace[]
    entry?: string
  }[]
  capabilities?: Capability[]
  network?: string[]
  permissions?: { id: string; minRole: HouseholdRole }[]
  jarvis?: boolean
  /** What JARVIS may do with the add-on's data, as `{id}_{name}` tools (docs/homemix.md, "JARVIS"). */
  tools?: AddonTool[]
  /** A collection JARVIS reads as `{id}_summary`. */
  summary?: { collection: string; scope?: 'household' | 'user'; description?: string }
}

export type ToolAction = 'add' | 'list' | 'get' | 'update' | 'delete' | 'script'
export type ToolFieldType = 'string' | 'integer' | 'number' | 'boolean'

export interface AddonTool {
  name: string
  description: string
  /** Not for script tools: they reach data through ctx. */
  collection?: string
  /** script runs tools/{name}.js on the server with a ToolContext (docs/homemix.md, "JARVIS"). */
  action: ToolAction
  scope?: 'household' | 'user'
  /** A script that only reads: JARVIS calls it without asking, and its writes fail. */
  readOnly?: boolean
  /** Only for add, update and script. */
  fields?: Record<string, { type: ToolFieldType; description?: string; required?: boolean; default?: unknown }>
  permission?: string
  /** What JARVIS says after a change; {key} and {field} are filled in. */
  answer?: string
}

export const RESERVED_IDS = ['households', 'accounts', 'jarvis', 'homemix', 'notifications', 'auth']
export const CAPABILITIES: Capability[] = ['storage.household', 'storage.user', 'household.members', 'notify']
export const ALLOWED_EXTENSIONS = ['.html', '.js', '.mjs', '.css', '.json', '.svg', '.png', '.webp', '.woff2', '.md']
export const MAX_FILES = 500
export const MAX_UNPACKED_BYTES = 25 * 1024 * 1024

const id = /^[a-z][a-z0-9-]{1,30}$/
const name = /^[a-z][a-z0-9._-]{0,63}$/
const version = /^\d{1,5}\.\d{1,5}\.\d{1,5}(-[0-9A-Za-z.-]{1,32})?$/
const path = /^[A-Za-z0-9._/-]{0,200}$/
const roles = ['Guest', 'Member', 'Admin', 'Owner']
const toolName = /^[a-z][a-z0-9_]{0,39}$/
const fieldName = /^[a-z][A-Za-z0-9_]{0,39}$/
const actions = ['add', 'list', 'get', 'update', 'delete', 'script']
const fieldTypes = ['string', 'integer', 'number', 'boolean']
const collection = /^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/

/** Every problem with a homemix.json, as the server reports them; empty when it is valid. */
export function checkManifest(json: unknown): string[] {
  const m = json as Partial<HomeMixManifest> & Record<string, unknown>
  const problems: string[] = []
  if (!m || typeof m !== 'object') return ['homemix.json must be a JSON object.']
  if (m.homemixVersion !== 1) problems.push('homemixVersion must be 1.')
  if (typeof m.id !== 'string' || !id.test(m.id))
    problems.push("id must be 2-31 characters: lowercase letters, digits and '-', starting with a letter.")
  else if (RESERVED_IDS.includes(m.id)) problems.push(`id '${m.id}' is reserved.`)
  if (typeof m.version !== 'string' || !version.test(m.version)) problems.push('version must look like 1.0.0.')
  if (typeof m.requires?.homeApps !== 'string' || !version.test(m.requires.homeApps))
    problems.push('requires.homeApps must be a version such as 2.3.0.')
  text(m.name, 'name', true, problems)
  text(m.description, 'description', false, problems)
  if (m.icon !== undefined && (typeof m.icon !== 'string' || !m.icon.startsWith('pi-')))
    problems.push('icon must be a PrimeIcons name such as pi-stopwatch.')
  if (
    m.targets !== undefined &&
    (!Array.isArray(m.targets) || !m.targets.length || m.targets.some((t) => t !== 'Household' && t !== 'User'))
  )
    problems.push('targets must name at least one of Household, User (or be left out for both).')
  htmlPath(m.entry, 'entry', true, problems)
  for (const page of m.pages ?? []) {
    if (typeof page.path !== 'string' || !path.test(page.path) || page.path.includes('..'))
      problems.push(`pages: '${page.path}' is not a valid page path.`)
    text(page.title, `pages['${page.path}'].title`, true, problems)
  }
  for (const widget of m.widgets ?? []) {
    if (typeof widget.id !== 'string' || !name.test(widget.id))
      problems.push(`widgets: '${widget.id}' is not a valid widget id.`)
    text(widget.title, `widgets['${widget.id}'].title`, true, problems)
    if (widget.size && !['Small', 'Medium', 'Large'].includes(widget.size))
      problems.push(`widgets['${widget.id}'].size must be Small, Medium or Large.`)
    for (const place of widget.places ?? [])
      if (!['Household', 'Wall', 'Account'].includes(place))
        problems.push(`widgets['${widget.id}'].places: '${place}' must be Household, Wall or Account.`)
    htmlPath(widget.entry, `widgets['${widget.id}'].entry`, false, problems)
  }
  for (const c of m.capabilities ?? [])
    if (!CAPABILITIES.includes(c)) problems.push(`capabilities: '${c}' is not a capability the bridge offers.`)
  for (const origin of m.network ?? [])
    if (!isHttpsOrigin(origin))
      problems.push(
        `network: '${origin}' must be an https origin without a path or wildcards, e.g. https://api.open-meteo.com.`,
      )
  for (const p of m.permissions ?? []) {
    if (typeof p.id !== 'string' || !name.test(p.id))
      problems.push(`permissions: '${p.id}' is not a valid permission id.`)
    if (!roles.includes(p.minRole))
      problems.push(`permissions['${p.id}'].minRole must be Guest, Member, Admin or Owner.`)
  }
  checkTools(m, problems)
  return problems
}

function checkTools(m: Partial<HomeMixManifest>, problems: string[]) {
  if ((m.tools?.length || m.summary) && m.jarvis !== true)
    problems.push('tools and summary are for JARVIS: set jarvis to true.')
  if ((m.tools?.length ?? 0) > 20) problems.push('tools: at most 20.')
  const scope = (s: unknown, at: string) => {
    if (s !== undefined && s !== 'household' && s !== 'user') problems.push(`${at}.scope must be household or user.`)
    else if (!m.capabilities?.includes(`storage.${s ?? 'household'}` as Capability))
      problems.push(`${at}.scope ${s ?? 'household'} needs the capability storage.${s ?? 'household'}.`)
  }
  for (const tool of m.tools ?? []) {
    const at = `tools['${tool.name}']`
    if (typeof tool.name !== 'string' || !toolName.test(tool.name) || tool.name === 'summary')
      problems.push(`${at}.name must be 1-40 characters: lowercase letters, digits and '_', starting with a letter, not 'summary'.`)
    else if (`${m.id}_${tool.name}`.length > 64) problems.push(`${at}.name: '${m.id}_${tool.name}' is longer than 64 characters.`)
    if (typeof tool.description !== 'string' || !tool.description.trim() || tool.description.length > 500)
      problems.push(`${at}.description is required, at most 500 characters: JARVIS picks tools by it.`)
    if (!actions.includes(tool.action)) problems.push(`${at}.action must be add, list, get, update, delete or script.`)
    const script = tool.action === 'script'
    if (!script && (typeof tool.collection !== 'string' || !collection.test(tool.collection)))
      problems.push(`${at}.collection must be a collection name.`)
    if (!script) scope(tool.scope, at)
    if (tool.readOnly !== undefined && !script)
      problems.push(`${at}.readOnly is for script tools; the others say it by their action.`)
    const fields = Object.entries(tool.fields ?? {})
    if (fields.length && tool.action !== 'add' && tool.action !== 'update' && !script)
      problems.push(`${at}.fields: only add, update and script tools take fields.`)
    if (fields.length > 20) problems.push(`${at}.fields: at most 20.`)
    for (const [field, spec] of fields) {
      if (!fieldName.test(field) || field === 'key' || field === 'household')
        problems.push(`${at}.fields.${field}: a field name is 1-40 letters, digits and '_', starting with a lowercase letter, not key or household.`)
      if (!fieldTypes.includes(spec?.type)) problems.push(`${at}.fields.${field}.type must be string, integer, number or boolean.`)
    }
    if (tool.permission !== undefined && !m.permissions?.some((p) => p.id === tool.permission))
      problems.push(`${at}.permission: '${tool.permission}' is not in permissions.`)
    if (tool.answer !== undefined && (typeof tool.answer !== 'string' || tool.answer.length > 300))
      problems.push(`${at}.answer: at most 300 characters.`)
  }
  if (m.summary) {
    if (typeof m.summary.collection !== 'string' || !collection.test(m.summary.collection))
      problems.push('summary.collection must be a collection name.')
    scope(m.summary.scope, 'summary')
  }
}

/** Every problem with the files of a package; paths relative to its root, with sizes. */
export function checkFiles(files: { path: string; size: number }[], manifest: HomeMixManifest): string[] {
  const problems: string[] = []
  if (files.length > MAX_FILES) problems.push(`A package may hold at most ${MAX_FILES} files.`)
  if (files.reduce((sum, f) => sum + f.size, 0) > MAX_UNPACKED_BYTES)
    problems.push('Unpacked, a package may have at most 25 MB.')
  for (const f of files) {
    if (!isSafePath(f.path))
      problems.push(`'${f.path}' is not allowed: no absolute paths, '..', backslashes or hidden files.`)
    else if (!ALLOWED_EXTENSIONS.includes(f.path.slice(f.path.lastIndexOf('.')).toLowerCase()))
      problems.push(`'${f.path}' is not allowed: only ${ALLOWED_EXTENSIONS.join(', ')} files.`)
  }
  const paths = new Set(files.map((f) => f.path))
  const scripts = (manifest.tools ?? []).filter((t) => t.action === 'script').map((t) => `tools/${t.name}.js`)
  for (const entry of new Set([manifest.entry, ...(manifest.widgets ?? []).map((w) => w.entry), ...scripts].filter(Boolean)))
    if (!paths.has(entry!)) problems.push(`'${entry}' is named in homemix.json but is not in the package.`)
  return problems
}

export function isSafePath(p: string) {
  return (
    !!p &&
    p.length <= 200 &&
    !p.startsWith('/') &&
    !p.includes('\\') &&
    !p.includes(':') &&
    p.split('/').every((s) => s.length > 0 && s !== '..' && s !== '.' && !s.startsWith('.'))
  )
}

function text(value: unknown, field: string, required: boolean, problems: string[]) {
  if (value === undefined) {
    if (required) problems.push(`${field} is required.`)
    return
  }
  const t = value as LocalizedText
  if (
    typeof t?.pl !== 'string' ||
    !t.pl.trim() ||
    t.pl.length > 200 ||
    (t.en !== undefined && (typeof t.en !== 'string' || t.en.length > 200))
  )
    problems.push(`${field} needs a Polish text (pl) of at most 200 characters, and optionally en.`)
}

function htmlPath(value: unknown, field: string, required: boolean, problems: string[]) {
  if (value === undefined || value === '') {
    if (required) problems.push(`${field} is required.`)
    return
  }
  const p = value as string
  if (typeof p !== 'string' || !path.test(p) || p.startsWith('/') || p.includes('..') || !p.endsWith('.html'))
    problems.push(`${field} must be a relative path to an .html file in the package.`)
}

function isHttpsOrigin(origin: string) {
  if (typeof origin !== 'string' || origin.includes('*')) return false
  try {
    const url = new URL(origin)
    return (
      url.protocol === 'https:' &&
      !url.username &&
      !url.password &&
      url.pathname === '/' &&
      !url.search &&
      !url.hash &&
      origin.replace(/\/$/, '').toLowerCase() === url.origin.toLowerCase()
    )
  } catch {
    return false
  }
}
