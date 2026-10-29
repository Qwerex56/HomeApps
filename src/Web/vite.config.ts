import { fileURLToPath, URL } from 'node:url'
import { defineConfig, type Plugin } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'
import pkg from './package.json' with { type: 'json' }

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
  /^\/plugins\//,
]

/** HOMEAPPS_PLUGINS="todo,weather" → ["todo","weather"]; the same rule as the production image (docker/40-plugins-json.sh). */
function pluginIds(list: string | undefined) {
  return (list ?? '')
    .split(',')
    .map((id) => id.trim())
    .filter((id) => /^[a-z][a-z0-9-]{1,31}$/.test(id))
}

// The instance's plugins (docs/plugins.md): during development from HOMEAPPS_PLUGINS, like nginx in production.
function pluginList(): Plugin {
  return {
    name: 'homeapps-plugin-list',
    configureServer(server) {
      server.middlewares.use('/plugins.json', (_request, response) => {
        response.setHeader('Content-Type', 'application/json')
        response.end(JSON.stringify(pluginIds(process.env.HOMEAPPS_PLUGINS)))
      })
    },
  }
}

export default defineConfig({
  plugins: [
    vue(),
    pluginList(),
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
        // A long press on the installed app's icon opens the wall view directly.
        shortcuts: [{ name: 'Tablet na ścianie', short_name: 'Tablet', url: '/wall' }],
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
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version),
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      // The plugin SDK lives next to the app; the app uses its source, plugins its packed build (scripts/pack.sh).
      '@homeapps/plugin-sdk': fileURLToPath(new URL('./plugin-sdk/src', import.meta.url)),
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
