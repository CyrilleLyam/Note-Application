<script setup lang="ts">
import { computed } from 'vue'
import { renderMarkdown } from '@/core/utils/markdown'

const props = defineProps<{
  source: string
}>()

const html = computed(() => renderMarkdown(props.source))
</script>

<template>
  <div
    class="prose prose-sm max-w-none break-words dark:prose-invert prose-a:text-primary prose-pre:bg-muted prose-pre:text-foreground prose-code:before:content-none prose-code:after:content-none"
    v-html="html"
  />
</template>
