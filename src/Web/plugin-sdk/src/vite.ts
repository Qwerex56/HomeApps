import { resolve } from 'node:path'
import type { Plugin } from 'vite'

/**
 * Modules the web app shares with plugins. A plugin must use the app's copies: one Vue means one reactivity system,
 * one router, one i18n and one query cache. The app puts them on globalThis.__HOMEAPPS_SHARED__ (src/main.ts).
 */
export const SHARED_MODULES = ['vue', 'vue-router', 'vue-i18n', 'pinia', '@tanstack/vue-query'] as const

const PREFIX = '\0homeapps-shared:'
const IDENTIFIER = /^[A-Za-z_$][\w$]*$/

export interface HomeAppsPluginOptions {
  /** The module that default-exports definePlugin({...}). Default: src/main.ts. */
  entry?: string
  /** Where the .NET project serves it from (UseHomeAppsPlugin): its wwwroot/ui. */
  outDir: string
}

/**
 * Builds a plugin's UI the way the web app loads it (docs/plugins.md, "UI"): one ES module ui/entry.js (+ entry.css
 * and lazy chunks next to it), with the shared modules read from the app instead of bundled.
 *
 * ```ts
 * export default defineConfig({ plugins: [vue(), homeAppsPlugin({ outDir: '../Todo.Api/wwwroot/ui' })] })
 * ```
 */
export function homeAppsPlugin(options: HomeAppsPluginOptions): Plugin {
  return {
    name: 'homeapps-plugin',
    apply: 'build',
    // Before Vite's own resolver, which would bundle the plugin's copy of Vue.
    enforce: 'pre',
    config: () => ({
      define: { 'process.env.NODE_ENV': JSON.stringify('production') },
      build: {
        outDir: options.outDir,
        emptyOutDir: true,
        cssCodeSplit: false,
        lib: {
          entry: resolve(options.entry ?? 'src/main.ts'),
          formats: ['es'],
          fileName: () => 'entry.js',
          cssFileName: 'entry',
        },
      },
    }),
    resolveId(id) {
      return (SHARED_MODULES as readonly string[]).includes(id) ? PREFIX + id : null
    },
    async load(id) {
      if (!id.startsWith(PREFIX)) return null
      const name = id.slice(PREFIX.length)
      // The export names come from the plugin's own copy of the package, which it has for types anyway.
      const exports = Object.keys(await import(name)).filter(
        (key) => key !== 'default' && key !== '__esModule' && IDENTIFIER.test(key),
      )
      return [
        `const m = globalThis.__HOMEAPPS_SHARED__?.[${JSON.stringify(name)}];`,
        `if (!m) throw new Error(${JSON.stringify(`HomeApps does not share ${name}; is this plugin loaded by the HomeApps web app?`)});`,
        'export default m.default ?? m;',
        `export const { ${exports.join(', ')} } = m;`,
      ].join('\n')
    },
  }
}
