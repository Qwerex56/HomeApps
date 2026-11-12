import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Relative paths: HomeApps serves the add-on at /homemix/run/<installation>/. public/homemix.json lands in dist.
export default defineConfig({
  base: './',
  plugins: [vue()],
  build: { outDir: 'dist', emptyOutDir: true, assetsInlineLimit: 0 },
})
