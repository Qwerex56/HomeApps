import MarkdownIt from 'markdown-it'

// Markdown from someone else's repository (homemix-2.md S5): no HTML (it is escaped), no images from outside (an
// image shows its alt text), links only http(s) and mailto, opened in a new tab without the opener.
const md = new MarkdownIt({ html: false, linkify: true, typographer: false })

const safeLink = /^(https?:|mailto:)/i
md.validateLink = (url) => safeLink.test(url.trim())

md.renderer.rules.image = (tokens, idx) => md.utils.escapeHtml(tokens[idx]!.content)

const defaultLinkOpen =
  md.renderer.rules.link_open ?? ((tokens, idx, options, _env, self) => self.renderToken(tokens, idx, options))
md.renderer.rules.link_open = (tokens, idx, options, env, self) => {
  tokens[idx]!.attrSet('target', '_blank')
  tokens[idx]!.attrSet('rel', 'noopener noreferrer nofollow')
  return defaultLinkOpen(tokens, idx, options, env, self)
}

/** HTML for `v-html`, safe because nothing from the source passes through as HTML. */
export function renderMarkdown(source: string): string {
  return md.render(source)
}
