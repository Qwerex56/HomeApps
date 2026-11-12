import { defineConfig } from 'vite'

// Relative paths: HomeApps serves the add-on at /homemix/run/<installation>/. public/homemix.json lands in dist.
export default defineConfig({
  base: './',
  build: { outDir: 'dist', emptyOutDir: true, assetsInlineLimit: 0 },
})
