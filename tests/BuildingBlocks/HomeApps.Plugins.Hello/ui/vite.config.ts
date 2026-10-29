import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { homeAppsPlugin } from '@homeapps/plugin-sdk/vite'

// The sample plugin's UI, built the way every plugin builds it (docs/plugins.md, "UI"). The output is committed,
// so the kit's .NET tests and the web app's checks need no npm here.
export default defineConfig({
  plugins: [vue(), homeAppsPlugin({ outDir: '../wwwroot/ui' })],
})
