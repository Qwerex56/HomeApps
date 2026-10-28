import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

// Paths that belong to the services behind Traefik. The service worker must never answer them with index.html,
// above all /auth/external/google, which is a full page navigation to Auth (frontend.md, section 4).
const servicePaths = [
  /^\/auth\//,
  /^\/\.well-known\//,
  /^\/users(\/|$)/,
  /^\/admin\/users(\/|$)/,
  /^\/households(\/|$)/,
  /^\/invites(\/|$)/,
  /^\/admin\/households(\/|$)/,
  /^\/openapi\//,
]

export default defineConfig({
  plugins: [
    vue(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'apple-touch-icon.png'],
      manifest: {
        name: 'HomeApps',
        short_name: 'HomeApps',
        description: 'Twój inteligentny dom',
        lang: 'pl',
        start_url: '/',
        display: 'standalone',
        background_color: '#0f172a',
        theme_color: '#0f172a',
        icons: [
          { src: 'pwa-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'pwa-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      // Only the app shell is cached; data always comes from the network (Q7).
      workbox: {
        navigateFallback: '/index.html',
        navigateFallbackDenylist: servicePaths,
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  // Traefik serves the app at https://app.localhost and reaches vite through host.docker.internal:5173.
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    allowedHosts: ['app.localhost'],
    hmr: { host: 'app.localhost', protocol: 'wss', clientPort: 443 },
  },
})
