import { defineConfig, devices } from '@playwright/test'

// Smoke test of a running HomeApps in a real browser (docs/web.md, "Smoke test"). Nothing is started here: point
// E2E_BASE_URL at a running stack (dev through Traefik, or the production compose).
export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  retries: 0,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'https://app.localhost',
    // The dev and smoke stacks use a local certificate.
    ignoreHTTPSErrors: true,
    locale: 'pl-PL',
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
    { name: 'phone', use: { ...devices['Pixel 7'] } },
  ],
})
