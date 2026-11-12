import { readdir, readFile, stat } from 'node:fs/promises'
import { join, relative, sep } from 'node:path'

/** Every file under dir, with paths as the package will hold them (forward slashes). */
export async function listFiles(dir) {
  const files = []
  async function walk(current) {
    for (const entry of await readdir(current, { withFileTypes: true })) {
      const full = join(current, entry.name)
      if (entry.isDirectory()) await walk(full)
      else if (entry.isFile())
        files.push({ path: relative(dir, full).split(sep).join('/'), size: (await stat(full)).size, full })
    }
  }
  await walk(dir)
  return files.sort((a, b) => a.path.localeCompare(b.path))
}

export async function readManifest(dir) {
  try {
    return JSON.parse(await readFile(join(dir, 'homemix.json'), 'utf8'))
  } catch (error) {
    throw new Error(`No readable homemix.json in ${dir}: ${error.message}`)
  }
}
