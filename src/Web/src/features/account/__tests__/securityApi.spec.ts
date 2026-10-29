import { describe, expect, it } from 'vitest'
import { describeUserAgent } from '../securityApi'

describe('describeUserAgent', () => {
  it.each([
    [
      'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36',
      'Chrome, macOS',
    ],
    [
      'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile Safari/604.1',
      'Safari, iOS',
    ],
    ['Mozilla/5.0 (Linux; Android 15) AppleWebKit/537.36 Chrome/130.0 Mobile Safari/537.36', 'Chrome, Android'],
    [
      'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130.0 Safari/537.36 Edg/130.0',
      'Edge, Windows',
    ],
    ['Mozilla/5.0 (X11; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0', 'Firefox, Linux'],
  ])('%s', (userAgent, expected) => {
    expect(describeUserAgent(userAgent)).toBe(expected)
  })

  it('keeps an unknown agent short and shows nothing for none', () => {
    expect(describeUserAgent('jarvis/1.0')).toBe('jarvis/1.0')
    expect(describeUserAgent(null)).toBeNull()
  })
})
