import { createHash } from 'node:crypto'
import { readFile, writeFile } from 'node:fs/promises'
import { crc32, deflateRawSync } from 'node:zlib'
import { check } from './check.js'
import { listFiles } from './files.js'

/** Checks the folder and writes addon.zip with homemix.json at its root; the same bytes give the same SHA-256. */
export async function pack(dir, out) {
  const problems = await check(dir)
  if (problems.length) throw new Error(`Not packed:\n${problems.map((p) => `  - ${p}`).join('\n')}`)

  const entries = []
  for (const file of await listFiles(dir)) entries.push({ name: file.path, data: await readFile(file.full) })
  const zip = createZip(entries)
  await writeFile(out, zip)
  return { out, sha256: createHash('sha256').update(zip).digest('hex'), size: zip.length }
}

// A plain zip writer (deflate, no extras, a fixed date so packing twice gives the same file).
function createZip(entries) {
  const locals = []
  const centrals = []
  let offset = 0
  const time = 0
  const date = ((2026 - 1980) << 9) | (1 << 5) | 1
  for (const { name, data } of entries) {
    const nameBytes = Buffer.from(name, 'utf8')
    const compressed = deflateRawSync(data, { level: 9 })
    const crc = crc32(data)
    const local = Buffer.alloc(30)
    local.writeUInt32LE(0x04034b50, 0)
    local.writeUInt16LE(20, 4)
    local.writeUInt16LE(0x0800, 6)
    local.writeUInt16LE(8, 8)
    local.writeUInt16LE(time, 10)
    local.writeUInt16LE(date, 12)
    local.writeUInt32LE(crc, 14)
    local.writeUInt32LE(compressed.length, 18)
    local.writeUInt32LE(data.length, 22)
    local.writeUInt16LE(nameBytes.length, 26)
    locals.push(local, nameBytes, compressed)

    const central = Buffer.alloc(46)
    central.writeUInt32LE(0x02014b50, 0)
    central.writeUInt16LE(20, 4)
    central.writeUInt16LE(20, 6)
    central.writeUInt16LE(0x0800, 8)
    central.writeUInt16LE(8, 10)
    central.writeUInt16LE(time, 12)
    central.writeUInt16LE(date, 14)
    central.writeUInt32LE(crc, 16)
    central.writeUInt32LE(compressed.length, 20)
    central.writeUInt32LE(data.length, 24)
    central.writeUInt16LE(nameBytes.length, 28)
    central.writeUInt32LE(offset, 42)
    centrals.push(central, nameBytes)
    offset += 30 + nameBytes.length + compressed.length
  }
  const centralSize = centrals.reduce((n, b) => n + b.length, 0)
  const end = Buffer.alloc(22)
  end.writeUInt32LE(0x06054b50, 0)
  end.writeUInt16LE(entries.length, 8)
  end.writeUInt16LE(entries.length, 10)
  end.writeUInt32LE(centralSize, 12)
  end.writeUInt32LE(offset, 16)
  return Buffer.concat([...locals, ...centrals, end])
}
