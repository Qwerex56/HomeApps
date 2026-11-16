import { describe, expect, it } from 'vitest'
import { renderMarkdown } from '../markdown'

// A README from someone else's repository (homemix-2.md S5).
describe('renderMarkdown', () => {
  it('renders Markdown', () => {
    const html = renderMarkdown('# Tytuł\n\n**gruby** i `kod`\n\n- jeden\n- dwa')

    expect(html).toContain('<h1>Tytuł</h1>')
    expect(html).toContain('<strong>gruby</strong>')
    expect(html).toContain('<code>kod</code>')
    expect(html).toContain('<li>jeden</li>')
  })

  it('escapes HTML instead of passing it through', () => {
    const html = renderMarkdown('<script>alert(1)</script>\n\n<img src=x onerror=alert(1)>')

    expect(html).not.toContain('<script>')
    expect(html).not.toContain('<img')
    expect(html).toContain('&lt;script&gt;')
  })

  it('shows an image as its alt text, never loading it', () => {
    const html = renderMarkdown('![zrzut ekranu](https://tracker.example.com/pixel.png)')

    expect(html).not.toContain('<img')
    expect(html).not.toContain('tracker.example.com')
    expect(html).toContain('zrzut ekranu')
  })

  it('opens http links in a new tab and drops javascript links', () => {
    const html = renderMarkdown('[repo](https://github.com/a/b) [zło](javascript:alert(1))')

    expect(html).toContain('href="https://github.com/a/b" target="_blank" rel="noopener noreferrer nofollow"')
    expect(html).not.toContain('href="javascript')
  })
})
