import { checkFiles, checkManifest } from '@homemix/sdk'
import { listFiles, readManifest } from './files.js'

/** The problems HomeApps would report when this folder is added as an add-on. */
export async function check(dir) {
  const manifest = await readManifest(dir)
  const problems = checkManifest(manifest)
  if (problems.length) return problems
  return checkFiles(await listFiles(dir), manifest)
}
