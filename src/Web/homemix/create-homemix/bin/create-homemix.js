#!/usr/bin/env node
// npm create homemix@latest my-addon [-- --template=vue|vanilla]
import { cp, readFile, readdir, rename, stat, writeFile } from 'node:fs/promises'
import { join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const args = process.argv.slice(2)
const name = args.find((a) => !a.startsWith('--'))
const template = args.find((a) => a.startsWith('--template='))?.split('=')[1] ?? 'vue'
const id = (name ?? '')
  .toLowerCase()
  .replace(/[^a-z0-9-]/g, '-')
  .replace(/^[^a-z]+/, '')

if (!name || !/^[a-z][a-z0-9-]{1,30}$/.test(id)) {
  console.error(
    'Usage: npm create homemix@latest <id> [-- --template=vue|vanilla]\n  id: 2-31 lowercase letters, digits or "-", starting with a letter.',
  )
  process.exit(1)
}
const source = fileURLToPath(new URL(`../templates/${template}/`, import.meta.url))
if (!(await stat(source).catch(() => null))) {
  console.error(`No template "${template}"; use vue or vanilla.`)
  process.exit(1)
}

const target = resolve(name)
if (await stat(target).catch(() => null)) {
  console.error(`${target} already exists.`)
  process.exit(1)
}
await cp(source, target, { recursive: true })

// npm drops .gitignore from published packages, so the template ships it as _gitignore.
await rename(join(target, '_gitignore'), join(target, '.gitignore')).catch(() => {})

async function fill(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name)
    if (entry.isDirectory()) await fill(full)
    else await writeFile(full, (await readFile(full, 'utf8')).replaceAll('__ID__', id))
  }
}
await fill(target)

console.log(`Created ${target}

  cd ${name}
  npm install
  npm run dev        a fake HomeApps at http://localhost:5180
  npm run pack       addon.zip and its SHA-256, ready to add in HomeApps

Publish: push to GitHub and tag v1.0.0; the workflow attaches addon.zip to the release.`)
