import { HomeMixError } from '@home-apps/sdk/host'

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/** A device id from an add-on frame goes into a request URL, so only a UUID gets through. */
export function checkDeviceId(id: unknown): string {
  if (typeof id !== 'string' || !uuid.test(id)) throw new HomeMixError('homemix.device_not_found', 'Unknown device.')
  return id
}
