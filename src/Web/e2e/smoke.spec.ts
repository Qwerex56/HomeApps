import { expect, test, type Page } from '@playwright/test'

// A signed-in walk through the app. The account comes from E2E_EMAIL / E2E_PASSWORD (for example the Owner), because
// registration is usually closed. With E2E_REGISTRATION_OPEN=1 a second person signs up and joins by an invite.
const email = process.env.E2E_EMAIL
const password = process.env.E2E_PASSWORD

test.skip(!email || !password, 'Set E2E_EMAIL and E2E_PASSWORD to an existing account.')

async function signIn(page: Page) {
  await page.goto('/login')
  await page.locator('#email').fill(email!)
  await page.locator('#password').fill(password!)
  await page.getByRole('button', { name: 'Zaloguj', exact: true }).click()
  await expect(page).toHaveURL(/\/$/)
}

async function createHousehold(page: Page, name: string) {
  await page.goto('/homes')
  await page.getByRole('button', { name: 'Nowy dom' }).click()
  await page.locator('#householdName').fill(name)
  await page.getByRole('button', { name: 'Utwórz', exact: true }).click()
  await expect(page).toHaveURL(/\/homes\/[^/]+\/members$/)
  return page.url().split('/homes/')[1]!.split('/')[0]!
}

async function archive(page: Page, id: string) {
  await page.goto(`/homes/${id}/settings`)
  await page.getByRole('button', { name: 'Zarchiwizuj dom' }).click()
  await page.locator('.p-confirmdialog').getByRole('button', { name: 'Zarchiwizuj dom' }).click()
  await expect(page).toHaveURL(/\/homes$/)
}

test('signs in, keeps the session over a reload and shows the dashboard', async ({ page }) => {
  await signIn(page)
  await expect(page.getByRole('heading', { name: /^Cześć/ })).toBeVisible()
  await page.reload()
  await expect(page.getByRole('heading', { name: /^Cześć/ })).toBeVisible()
  await page.goto('/account')
  await expect(page.locator('#displayName')).not.toHaveValue('')
})

test('creates a household, invites, shows it on the wall and archives it', async ({ page }) => {
  const name = `Smoke ${Date.now()}`
  await signIn(page)
  const id = await createHousehold(page, name)

  await page.goto(`/homes/${id}/invites`)
  await page.getByRole('button', { name: 'Utwórz zaproszenie' }).click()
  const code = (await page.locator('section li .font-mono').first().innerText()).trim()
  expect(code).toMatch(/^[0-9A-Z]{5}-[0-9A-Z]{5}$/)

  // The code leads to this household; I am already in it, so joining just opens it.
  await page.goto(`/join/${code}`)
  await expect(page.getByText(name)).toBeVisible()
  await page.getByRole('button', { name: 'Dołącz' }).click()
  await expect(page).toHaveURL(new RegExp(`/homes/${id}/members$`))

  await page.goto('/wall')
  await expect(page.getByRole('heading', { name })).toBeVisible()
  await expect(page.locator('time').first()).toHaveText(/^\d{2}:\d{2}$/)

  await archive(page, id)
})

test('a second person signs up from an invite link and joins', async ({ page, browser }) => {
  test.skip(process.env.E2E_REGISTRATION_OPEN !== '1', 'Registration is closed on this server.')
  const name = `Smoke ${Date.now()}`
  await signIn(page)
  const id = await createHousehold(page, name)
  await page.goto(`/homes/${id}/invites`)
  await page.getByRole('button', { name: 'Utwórz zaproszenie' }).click()
  const code = (await page.locator('section li .font-mono').first().innerText()).trim()

  const guest = await (await browser.newContext({ ignoreHTTPSErrors: true })).newPage()
  await guest.goto(`/join/${code}`)
  await expect(guest).toHaveURL(/\/login/)
  await guest.getByRole('link', { name: 'Załóż je' }).click()
  await guest.locator('#displayName').fill('Smoke')
  await guest.locator('#email').fill(`smoke${Date.now()}@example.com`)
  await guest.locator('#password').fill('Smoke-Test-Haslo-2026')
  await guest.getByRole('button', { name: 'Załóż konto' }).click()
  await expect(guest).toHaveURL(new RegExp(`/join/${code}`))
  await guest.getByRole('button', { name: 'Dołącz' }).click()
  await expect(guest).toHaveURL(new RegExp(`/homes/${id}/members$`))

  await page.goto(`/homes/${id}/members`)
  await expect(page.locator('section li')).toHaveCount(2)
  await archive(page, id)
})
