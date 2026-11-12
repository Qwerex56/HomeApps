import { createServer } from 'node:http'
import { readFile } from 'node:fs/promises'
import { dirname, extname, join, normalize, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import { isSafePath } from '@homemix/sdk'
import { readManifest } from './files.js'

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.webp': 'image/webp',
  '.woff2': 'font/woff2',
  '.md': 'text/markdown; charset=utf-8',
}

const hostDir = fileURLToPath(new URL('../host/', import.meta.url))
const sdkDir = dirname(fileURLToPath(import.meta.resolve('@homemix/sdk')))

/** The sandbox headers HomeApps sends with an add-on's files (docs/homemix.md, "The sandbox"). */
export function sandboxHeaders(network) {
  return {
    'Content-Security-Policy': [
      'sandbox allow-scripts allow-forms allow-popups',
      "default-src 'self' 'unsafe-inline' data: blob:",
      "script-src 'self' 'unsafe-inline' blob:",
      `connect-src ${network.length ? network.join(' ') : "'none'"}`,
      "form-action 'none'",
      "base-uri 'none'",
      "frame-ancestors 'self'",
    ].join('; '),
    'Access-Control-Allow-Origin': '*',
    'X-Content-Type-Options': 'nosniff',
    'Cache-Control': 'no-cache',
  }
}

async function send(res, file, headers = {}) {
  try {
    const body = await readFile(file)
    res.writeHead(200, { 'Content-Type': types[extname(file)] ?? 'application/octet-stream', ...headers })
    res.end(body)
  } catch {
    res.writeHead(404).end()
  }
}

function inside(root, path) {
  const full = normalize(join(root, path))
  return full.startsWith(normalize(root + sep)) ? full : null
}

/** A fake HomeApps around the add-on in dir: rebuild it (vite build --watch) and reload the page. */
export async function dev(dir, port) {
  await readManifest(dir)
  const server = createServer(async (req, res) => {
    const url = new URL(req.url ?? '/', 'http://localhost')
    const path = decodeURIComponent(url.pathname)
    if (path === '/') return send(res, join(hostDir, 'index.html'))
    if (path === '/manifest.json') return send(res, join(dir, 'homemix.json'))
    if (path.startsWith('/host/')) {
      const file = inside(hostDir, path.slice(6))
      return file ? send(res, file) : res.writeHead(404).end()
    }
    if (path.startsWith('/sdk/')) {
      const file = inside(sdkDir, path.slice(5))
      return file ? send(res, file) : res.writeHead(404).end()
    }
    if (path.startsWith('/run/')) {
      const manifest = await readManifest(dir)
      const relativePath = path.slice(5) || manifest.entry
      if (!isSafePath(relativePath) || relativePath === 'homemix.json') return res.writeHead(404).end()
      return send(res, join(dir, relativePath), sandboxHeaders(manifest.network ?? []))
    }
    res.writeHead(404).end()
  })
  await new Promise((done) => server.listen(port, done))
  console.log(`HomeMix dev host: http://localhost:${port}  (add-on from ${dir})`)
}
