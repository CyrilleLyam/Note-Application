<script setup lang="ts">
import { X } from '@lucide/vue'
import { computed, ref, useAttrs, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { normalizeTag, NOTE_MAX_TAGS, NOTE_TAG_MAX_LENGTH } from '../models/validation'

defineOptions({
  inheritAttrs: false,
})

const props = defineProps<{
  suggestions?: string[]
  placeholder?: string
}>()

const model = defineModel<string[]>({ default: () => [] })

const { t } = useI18n()
const attrs = useAttrs()
const draft = ref('')
const inputRef = ref<HTMLInputElement | null>(null)
const suggestionListId = useId()

const inputAttrs = computed(() => {
  const { onInput, onChange, ...rest } = attrs
  return rest
})

const tags = computed(() => Array.isArray(model.value) ? model.value : [])
const isFull = computed(() => tags.value.length >= NOTE_MAX_TAGS)
const availableSuggestions = computed(() => (props.suggestions ?? []).filter(tag => !tags.value.includes(tag)))

function addTags(values: string[]) {
  const next = [...tags.value]
  for (const value of values) {
    const tag = normalizeTag(value)
    if (tag && !next.includes(tag) && next.length < NOTE_MAX_TAGS) {
      next.push(tag)
    }
  }
  if (next.length !== tags.value.length) {
    model.value = next
  }
}

function addTag(raw: string) {
  draft.value = ''
  addTags([raw])
}

watch(draft, (value) => {
  if (!value.includes(',')) {
    return
  }
  const parts = value.split(',')
  const remainder = parts.pop() ?? ''
  addTags(parts)
  draft.value = remainder
})

function removeTag(tag: string) {
  model.value = tags.value.filter(item => item !== tag)
  inputRef.value?.focus()
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
    if (draft.value.trim()) {
      addTag(draft.value)
    }
    return
  }

  if (event.key === 'Enter' || event.key === ',') {
    event.preventDefault()
    if (draft.value.trim()) {
      addTag(draft.value)
    }
    return
  }

  if (event.key === 'Backspace' && !draft.value && tags.value.length > 0) {
    event.preventDefault()
    removeTag(tags.value[tags.value.length - 1]!)
  }
}

function onBlur() {
  if (draft.value.trim()) {
    addTag(draft.value)
  }
}
</script>

<template>
  <div
    class="flex min-h-9 w-full flex-wrap items-center gap-1.5 rounded-md border border-input bg-transparent px-2 py-1.5 shadow-xs transition-[color,box-shadow] focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/50 dark:bg-input/30"
    @click="inputRef?.focus()"
  >
    <span
      v-for="tag in tags"
      :key="tag"
      class="inline-flex items-center gap-1 rounded-full bg-secondary py-0.5 pl-2.5 pr-1 text-xs font-medium text-secondary-foreground"
    >
      {{ tag }}
      <button
        type="button"
        class="rounded-full p-0.5 text-muted-foreground transition-colors hover:bg-background hover:text-foreground"
        :aria-label="t('notes.form.removeTag', { tag })"
        @click.stop="removeTag(tag)"
      >
        <X class="h-3 w-3" />
      </button>
    </span>
    <input
      ref="inputRef"
      v-bind="inputAttrs"
      v-model="draft"
      type="text"
      :list="suggestionListId"
      :disabled="isFull"
      :maxlength="NOTE_TAG_MAX_LENGTH"
      :placeholder="isFull ? '' : placeholder"
      class="min-w-24 flex-1 bg-transparent px-1 text-base outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed md:text-sm"
      @keydown="onKeydown"
      @blur="onBlur"
    >
    <datalist :id="suggestionListId">
      <option v-for="suggestion in availableSuggestions" :key="suggestion" :value="suggestion" />
    </datalist>
  </div>
</template>
