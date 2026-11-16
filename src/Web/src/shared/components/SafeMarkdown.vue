<script setup lang="ts">
import { computed } from 'vue'
import { renderMarkdown } from '@/shared/markdown'

// Markdown from outside (a README), rendered without HTML or outside images (src/shared/markdown.ts).
const props = defineProps<{ source: string }>()
const html = computed(() => renderMarkdown(props.source))
</script>

<template>
  <!-- eslint-disable-next-line vue/no-v-html -- renderMarkdown escapes all HTML and drops images -->
  <div class="markdown" v-html="html" />
</template>

<style scoped>
.markdown :deep(h1),
.markdown :deep(h2),
.markdown :deep(h3) {
  margin: 1rem 0 0.5rem;
  font-weight: 600;
}
.markdown :deep(h1) {
  font-size: 1.4rem;
}
.markdown :deep(h2) {
  font-size: 1.2rem;
}
.markdown :deep(p),
.markdown :deep(ul),
.markdown :deep(ol),
.markdown :deep(pre),
.markdown :deep(blockquote) {
  margin: 0.5rem 0;
}
.markdown :deep(ul) {
  padding-left: 1.5rem;
  list-style: disc;
}
.markdown :deep(ol) {
  padding-left: 1.5rem;
  list-style: decimal;
}
.markdown :deep(a) {
  color: var(--p-primary-color);
  text-decoration: underline;
}
.markdown :deep(code) {
  font-size: 0.875em;
  border-radius: 0.25rem;
  padding: 0 0.25rem;
  background: var(--p-content-hover-background);
}
.markdown :deep(pre) {
  overflow-x: auto;
  border-radius: 0.375rem;
  padding: 0.75rem;
  background: var(--p-content-hover-background);
}
.markdown :deep(pre code) {
  padding: 0;
}
.markdown :deep(blockquote) {
  border-left: 3px solid var(--p-content-border-color);
  padding-left: 0.75rem;
  color: var(--p-text-muted-color);
}
.markdown :deep(table) {
  border-collapse: collapse;
}
.markdown :deep(th),
.markdown :deep(td) {
  border: 1px solid var(--p-content-border-color);
  padding: 0.25rem 0.5rem;
}
</style>
