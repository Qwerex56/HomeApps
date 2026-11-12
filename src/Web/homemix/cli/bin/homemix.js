#!/usr/bin/env node
// homemix check | pack | dev (docs/homemix.md, "Building one"). Works on the built add-on: a folder with homemix.json.
import { resolve } from 'node:path'
import { check } from '../lib/check.js'
import { pack } from '../lib/pack.js'
import { dev } from '../lib/dev.js'

const [command, ...rest] = process.argv.slice(2)
const flags = Object.fromEntries(
  rest
    .filter((a) => a.startsWith('--'))
    .map((a) => {
      const [k, v] = a.slice(2).split('=')
      return [k, v ?? 'true']
    }),
)
const dir = resolve(rest.find((a) => !a.startsWith('--')) ?? 'dist')

const usage = `homemix <command> [dir]   (dir: the built add-on with homemix.json, default ./dist)

  check                     the checks HomeApps makes when the add-on is added
  pack [--out=addon.zip]    check, then zip it and print the SHA-256
  dev [--port=5180]         a fake HomeApps host with a household, roles and data in memory
`

try {
  if (command === 'check') {
    const problems = await check(dir)
    if (problems.length) {
      console.error(problems.map((p) => `  - ${p}`).join('\n'))
      process.exit(1)
    }
    console.log('homemix.json and the files are fine.')
  } else if (command === 'pack') {
    const { out, sha256, size } = await pack(dir, resolve(flags.out ?? 'addon.zip'))
    console.log(`${out}\n  ${(size / 1024).toFixed(1)} KB\n  SHA-256 ${sha256}`)
  } else if (command === 'dev') {
    await dev(dir, Number(flags.port ?? 5180))
  } else {
    console.log(usage)
    process.exit(command ? 1 : 0)
  }
} catch (error) {
  console.error(error instanceof Error ? error.message : error)
  process.exit(1)
}
