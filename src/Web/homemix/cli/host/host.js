// The fake host of `homemix dev`: the same bridge as HomeApps (serveFrame from @homemix/sdk), with a household, roles
// and data in memory, so an add-on runs without a HomeApps instance.
import { HomeMixError, emit, serveFrame } from '/sdk/host.js'

const manifest = await (await fetch('/manifest.json')).json()
const $ = (id) => document.getElementById(id)
const ranks = { Guest: 0, Member: 1, Admin: 2, Owner: 3 }
const me = { id: '00000000-0000-4000-8000-000000000001', name: 'Ty' }
const members = [
  { id: me.id, name: me.name, avatarUrl: null, role: 'Member' },
  { id: '00000000-0000-4000-8000-000000000002', name: 'Ewa', avatarUrl: null, role: 'Owner' },
  { id: '00000000-0000-4000-8000-000000000003', name: 'Adam', avatarUrl: null, role: 'Guest' },
]
const themes = {
  false: {
    '--p-primary-color': '#6366f1',
    '--p-primary-contrast-color': '#ffffff',
    '--p-text-color': '#334155',
    '--p-text-muted-color': '#64748b',
    '--p-content-background': '#ffffff',
    '--p-content-border-color': '#e2e8f0',
    '--p-surface-100': '#f1f5f9',
    '--p-surface-800': '#1e293b',
  },
  true: {
    '--p-primary-color': '#818cf8',
    '--p-primary-contrast-color': '#0f172a',
    '--p-text-color': '#ffffff',
    '--p-text-muted-color': '#a1a1aa',
    '--p-content-background': '#18181b',
    '--p-content-border-color': '#3f3f46',
    '--p-surface-100': '#f1f5f9',
    '--p-surface-800': '#27272a',
  },
}
let store = {}
let stop = null

$('title').textContent = `HomeMix: ${manifest.name?.pl ?? manifest.id} ${manifest.version}`
const views = [
  ...(manifest.pages ?? [{ path: '', title: manifest.name }]).map((p) => ({
    label: `Strona: ${p.title?.pl ?? p.path}`,
    page: p.path,
    file: '',
    widget: null,
  })),
  ...(manifest.widgets ?? []).flatMap((w) =>
    (w.places ?? ['Household']).map((place) => ({
      label: `Kafelek: ${w.title?.pl ?? w.id} (${place})`,
      page: '',
      file: w.entry ?? '',
      widget: { id: w.id, place },
    })),
  ),
]
$('view').innerHTML = views.map((v, i) => `<option value="${i}">${v.label}</option>`).join('')

const state = () => ({
  target: $('target').value,
  role: $('role').value,
  view: views[Number($('view').value)],
  dark: $('dark').checked,
})
const theme = () => ({ dark: state().dark, vars: themes[state().dark] })
const role = () => (state().target === 'Household' ? state().role : 'Owner')
const permissions = () => (manifest.permissions ?? []).filter((p) => ranks[role()] >= ranks[p.minRole]).map((p) => p.id)
const log = (line) => ($('log').textContent = `${line}\n${$('log').textContent}`.slice(0, 4000))
const show = () => ($('data').textContent = JSON.stringify(store, null, 1))
const space = (scope = 'household', collection) => `${scope === 'user' ? `user:${me.id}` : 'household'}/${collection}`

function writable(scope, collection) {
  if (state().target === 'User' || scope === 'user') return
  const named = (manifest.permissions ?? []).find((p) => p.id === `${collection}.write`)
  if (ranks[role()] < ranks[named?.minRole ?? 'Member'])
    throw new HomeMixError('homemix.read_only', "Your role in this household may only read this add-on's data.")
}

function frameWindow() {
  return $('frame').querySelector('iframe')?.contentWindow
}

const handlers = {
  context: () => ({
    addon: { id: manifest.id, name: manifest.name?.pl ?? manifest.id, version: manifest.version },
    installation: { id: '00000000-0000-4000-8000-0000000000aa', target: state().target },
    household:
      state().target === 'Household'
        ? { id: '00000000-0000-4000-8000-0000000000bb', name: 'Dom testowy', myRole: role() }
        : null,
    user: me,
    permissions: permissions(),
    capabilities: manifest.capabilities ?? [],
    language: 'pl',
    theme: theme(),
    page: state().view.page,
    widget: state().view.widget,
  }),
  can: ({ permission }) => permissions().includes(permission),
  resize: ({ height }) => {
    $('frame').querySelector('iframe').style.height = `${Math.min(Math.max(height, 40), 4000)}px`
    return null
  },
  navigate: ({ path }) => {
    const index = views.findIndex((v) => v.page === path && !v.widget)
    if (index >= 0) {
      $('view').value = String(index)
      load()
    }
    return null
  },
  'data.list': ({ collection, scope }) =>
    Object.values(store[space(scope, collection)] ?? {}).sort((a, b) => a.key.localeCompare(b.key)),
  'data.get': ({ collection, key, scope }) => store[space(scope, collection)]?.[key] ?? null,
  'data.put': ({ collection, key, value, scope, version }) => {
    writable(scope, collection)
    const docs = (store[space(scope, collection)] ??= {})
    const current = docs[key]
    if (version != null && current?.version !== version)
      throw new HomeMixError('homemix.conflict', 'Someone changed this document first.')
    docs[key] = {
      key,
      value,
      version: (current?.version ?? 0) + 1,
      updatedBy: me.id,
      updatedAt: new Date().toISOString(),
    }
    show()
    return docs[key]
  },
  'data.delete': ({ collection, key, scope, version }) => {
    writable(scope, collection)
    const docs = store[space(scope, collection)] ?? {}
    if (!docs[key]) throw new HomeMixError('homemix.document_not_found', 'There is no such document.')
    if (version != null && docs[key].version !== version)
      throw new HomeMixError('homemix.conflict', 'Someone changed this document first.')
    delete docs[key]
    show()
    return null
  },
  members: () =>
    state().target === 'Household' ? members.map((m) => (m.id === me.id ? { ...m, role: role() } : m)) : [members[0]],
  toast: ({ message }) => {
    const toast = document.createElement('div')
    toast.textContent = `${manifest.name?.pl ?? manifest.id}: ${message}`
    $('toasts').append(toast)
    setTimeout(() => toast.remove(), 4000)
    return null
  },
  // HomeApps sends it through Notifications; here it is only shown, with whom it would reach.
  'notify.push': ({ title, body, userIds }) => {
    const everyone = state().target === 'Household' ? members.map((m) => m.id) : [me.id]
    const recipients = userIds ?? everyone.filter((id) => id !== me.id || state().target !== 'Household')
    if (recipients.some((id) => !everyone.includes(id)))
      throw new HomeMixError('homemix.not_a_recipient', 'A notification can go only to people of this household.')
    const toast = document.createElement('div')
    toast.textContent = `🔔 ${title}: ${body} (→ ${recipients.length})`
    $('toasts').append(toast)
    setTimeout(() => toast.remove(), 4000)
    return { recipients: recipients.length }
  },
}

const logged = Object.fromEntries(
  Object.entries(handlers).map(([method, handler]) => [
    method,
    (params) => (log(`${method} ${JSON.stringify(params ?? {})}`), handler(params)),
  ]),
)

function load() {
  stop?.()
  const { view } = state()
  document.documentElement.style.colorScheme = state().dark ? 'dark' : 'light'
  $('frame').className = view.widget ? 'widget' : ''
  $('frame').innerHTML = ''
  const iframe = document.createElement('iframe')
  iframe.setAttribute('sandbox', 'allow-scripts allow-forms allow-popups')
  iframe.src = `/run/${view.file}${view.page ? `#/${view.page}` : ''}`
  $('frame').append(iframe)
  stop = serveFrame({ frame: frameWindow, capabilities: manifest.capabilities ?? [], handlers: logged })
}

for (const id of ['target', 'role', 'view']) $(id).addEventListener('change', load)
$('dark').addEventListener('change', () => emit(frameWindow(), 'theme', theme()))
$('reset').addEventListener('click', () => {
  store = {}
  show()
  load()
})
load()
