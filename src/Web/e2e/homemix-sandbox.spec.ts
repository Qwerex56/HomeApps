import { expect, test } from '@playwright/test'

// ADR 0009 / docs/homemix.md, "The sandbox": an add-on's frame, served with the headers HomeMix sends, can read nothing
// of HomeApps: no localStorage, no cookies, not the parent page, not the services with the person's session.
// Needs no running stack: both pages are answered here, on one origin, as Traefik would serve them.
const origin = 'https://homeapps.test'
const csp = [
  'sandbox allow-scripts allow-forms allow-popups',
  "default-src 'self' 'unsafe-inline' data: blob:",
  "script-src 'self' 'unsafe-inline' blob:",
  "connect-src 'none'",
  "form-action 'none'",
  "base-uri 'none'",
  "frame-ancestors 'self'",
].join('; ')

const host = `<!doctype html><title>host</title><script>
  localStorage.setItem('homeapps.session', 'secret-token')
  document.cookie = 'refresh=secret-cookie; path=/'
  window.results = new Promise((resolve) => addEventListener('message', (e) => resolve(e.data)))
</script><iframe sandbox="allow-scripts allow-forms allow-popups" src="/homemix/run/1/"></iframe>`

const addon = `<!doctype html><script>
  const tryIt = async (f) => { try { return String(await f()) } catch (e) { return 'blocked' } }
  ;(async () => parent.postMessage({
    origin: self.origin,
    storage: await tryIt(() => localStorage.getItem('homeapps.session')),
    cookie: await tryIt(() => document.cookie || 'blocked'),
    parentDocument: await tryIt(() => parent.document.title),
    parentStorage: await tryIt(() => parent.localStorage.getItem('homeapps.session')),
    service: await tryIt(async () => (await fetch('${origin}/households', { credentials: 'include' })).status),
    otherSite: await tryIt(async () => (await fetch('https://example.com/')).status),
  }, '*'))()
</script>`

async function serve(page: import('@playwright/test').Page) {
  await page.context().route(`${origin}/**`, (route) => {
    const path = new URL(route.request().url()).pathname
    if (path === '/') return route.fulfill({ contentType: 'text/html', body: host })
    if (path.startsWith('/homemix/run/'))
      return route.fulfill({
        contentType: 'text/html',
        headers: { 'Content-Security-Policy': csp, 'Access-Control-Allow-Origin': '*' },
        body: addon,
      })
    return route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
  })
}

test('an add-on frame reads nothing of HomeApps', async ({ page }) => {
  await serve(page)
  await page.goto(`${origin}/`)
  const results = await page.evaluate(() => (window as unknown as { results: Promise<unknown> }).results)

  expect(results).toEqual({
    origin: 'null',
    storage: 'blocked',
    cookie: 'blocked',
    parentDocument: 'blocked',
    parentStorage: 'blocked',
    service: 'blocked',
    otherSite: 'blocked',
  })
})

test('opened in a tab of its own, an add-on still has no origin', async ({ page }) => {
  await serve(page)
  await page.goto(`${origin}/`)
  const tab = await page.context().newPage()
  await tab.goto(`${origin}/homemix/run/1/`)

  const results = await tab.evaluate(async () => {
    const tryIt = (f: () => unknown) => {
      try {
        return String(f())
      } catch {
        return 'blocked'
      }
    }
    return { origin: self.origin, storage: tryIt(() => localStorage.getItem('homeapps.session')) }
  })

  expect(results).toEqual({ origin: 'null', storage: 'blocked' })
})
