import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'

const markdown = new MarkdownIt({
  html: false,
  linkify: true,
  breaks: true,
})

DOMPurify.addHook('afterSanitizeAttributes', (node) => {
  if (node.tagName === 'A') {
    node.setAttribute('target', '_blank')
    node.setAttribute('rel', 'noopener noreferrer')
  }
})

export function renderMarkdown(source: string): string {
  return DOMPurify.sanitize(markdown.render(source))
}

export function markdownToText(source: string): string {
  const container = document.createElement('div')
  container.innerHTML = renderMarkdown(source)
  return (container.textContent ?? '').replace(/\n{2,}/g, '\n').trim()
}
