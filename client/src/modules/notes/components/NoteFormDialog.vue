<script setup lang="ts">
import type { Note } from '../models/note'
import type { NoteDraft } from '../store/draftsStore'
import { AlertCircle, History, Loader2, TriangleAlert } from '@lucide/vue'
import { toTypedSchema } from '@vee-validate/zod'
import { watchDebounced } from '@vueuse/core'
import { storeToRefs } from 'pinia'
import { useForm } from 'vee-validate'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from 'vue-sonner'
import { Button } from '@/core/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/core/components/ui/dialog'
import { FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/core/components/ui/form'
import { Input } from '@/core/components/ui/input'
import { Textarea } from '@/core/components/ui/textarea'
import { describeError, isApiError } from '@/core/services/apiError'
import { formatRelative } from '@/core/utils/date'
import { useAuthStore } from '@/modules/auth'
import { NOTE_TITLE_MAX_LENGTH, noteSchema } from '../models/validation'
import { useDraftsStore } from '../store/draftsStore'
import { useNotesStore } from '../store/notesStore'
import MarkdownContent from './MarkdownContent.vue'
import TagInput from './TagInput.vue'

const props = defineProps<{
  note?: Note | null
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const notesStore = useNotesStore()
const draftsStore = useDraftsStore()
const { user } = storeToRefs(useAuthStore())
const { tags: allTags } = storeToRefs(notesStore)

const submitError = ref<string | null>(null)
const baseVersion = ref<string | null>(null)
const hasConflict = ref(false)
const isResolving = ref(false)
const contentMode = ref<'write' | 'preview'>('write')
const restoredDraft = ref<NoteDraft | null>(null)
const draftSavedAt = ref<Date | null>(null)
const formRef = ref<HTMLFormElement | null>(null)
let skipDraftOnClose = false

const { errors, handleSubmit, isSubmitting, meta, resetForm, setValues, values } = useForm({
  validationSchema: toTypedSchema(noteSchema),
  initialValues: {
    title: '',
    content: '',
    tags: [] as string[],
  },
})

const isEditing = computed(() => !!props.note)
const canSubmit = computed(() => !isSubmitting.value
  && !isResolving.value
  && !hasConflict.value
  && (!isEditing.value || meta.value.dirty))
const tagSuggestions = computed(() => allTags.value.map(tag => tag.name))
const draftKey = computed(() => user.value ? draftsStore.draftKey(user.value.id, props.note?.id) : null)

function noteValues() {
  return {
    title: props.note?.title ?? '',
    content: props.note?.content ?? '',
    tags: [...(props.note?.tags ?? [])],
  }
}

function currentValues() {
  return {
    title: values.title ?? '',
    content: values.content ?? '',
    tags: [...(values.tags ?? [])],
  }
}

function isSameAsNote(draft: Pick<NoteDraft, 'title' | 'content' | 'tags'>) {
  const initial = noteValues()
  return draft.title === initial.title
    && draft.content === initial.content
    && draft.tags.join('\n') === initial.tags.join('\n')
}

function persistDraft() {
  if (!draftKey.value) {
    return
  }
  if (!meta.value.dirty) {
    draftsStore.clearDraft(draftKey.value)
    draftSavedAt.value = null
    return
  }
  draftsStore.saveDraft(draftKey.value, { ...currentValues(), baseVersion: baseVersion.value })
  draftSavedAt.value = new Date()
}

function clearDraft() {
  if (draftKey.value) {
    draftsStore.clearDraft(draftKey.value)
  }
  restoredDraft.value = null
  draftSavedAt.value = null
}

watch(open, (isOpen, wasOpen) => {
  if (!isOpen) {
    if (wasOpen && !skipDraftOnClose) {
      persistDraft()
    }
    skipDraftOnClose = false
    return
  }

  submitError.value = null
  hasConflict.value = false
  contentMode.value = 'write'
  draftSavedAt.value = null
  baseVersion.value = props.note?.rowVersion ?? null
  resetForm({ values: noteValues() })

  const draft = draftKey.value ? draftsStore.getDraft(draftKey.value) : null
  if (draft && !isSameAsNote(draft)) {
    setValues({ title: draft.title, content: draft.content, tags: [...draft.tags] }, false)
    baseVersion.value = draft.baseVersion ?? baseVersion.value
    restoredDraft.value = draft
  }
  else {
    clearDraft()
  }
}, { immediate: true })

watchDebounced(values, () => {
  if (open.value && !isSubmitting.value) {
    persistDraft()
  }
}, { debounce: 600, deep: true })

function discardDraft() {
  clearDraft()
  baseVersion.value = props.note?.rowVersion ?? null
  resetForm({ values: noteValues() })
}

const onSubmit = handleSubmit(async (formValues) => {
  submitError.value = null

  const payload = {
    title: formValues.title,
    content: formValues.content?.trim() ? formValues.content : null,
    tags: formValues.tags,
  }

  try {
    if (props.note && baseVersion.value) {
      await notesStore.updateNote(props.note.id, { ...payload, rowVersion: baseVersion.value })
    }
    else {
      await notesStore.createNote(payload)
    }

    toast.success(props.note ? t('notes.toasts.updated') : t('notes.toasts.created'))
    skipDraftOnClose = true
    clearDraft()
    open.value = false
  }
  catch (err) {
    if (isApiError(err, 409)) {
      hasConflict.value = true
      return
    }
    submitError.value = describeError(err)
  }
})

async function resolveConflict(keepMine: boolean) {
  if (!props.note) {
    return
  }

  isResolving.value = true
  submitError.value = null

  try {
    const latest = await notesStore.refreshNote(props.note.id)
    baseVersion.value = latest.rowVersion
    hasConflict.value = false

    if (keepMine) {
      await onSubmit()
    }
    else {
      clearDraft()
      resetForm({
        values: {
          title: latest.title,
          content: latest.content ?? '',
          tags: [...latest.tags],
        },
      })
    }
  }
  catch (err) {
    submitError.value = describeError(err)
  }
  finally {
    isResolving.value = false
  }
}

function focusTitle(event: Event) {
  event.preventDefault()
  formRef.value?.querySelector<HTMLInputElement>('input')?.focus()
}

function submitWithShortcut() {
  if (canSubmit.value) {
    onSubmit()
  }
}
</script>

<template>
  <Dialog v-model:open="open">
    <DialogContent class="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-2xl" @open-auto-focus="focusTitle">
      <DialogHeader>
        <DialogTitle>{{ isEditing ? t('notes.form.editTitle') : t('notes.form.newTitle') }}</DialogTitle>
        <DialogDescription>
          {{ isEditing ? t('notes.form.editDescription') : t('notes.form.newDescription') }}
        </DialogDescription>
      </DialogHeader>

      <form
        id="note-form"
        ref="formRef"
        novalidate
        class="space-y-4"
        @submit="onSubmit"
        @keydown.ctrl.enter.prevent="submitWithShortcut"
        @keydown.meta.enter.prevent="submitWithShortcut"
      >
        <div
          v-if="restoredDraft"
          class="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-primary/20 bg-primary/5 px-3 py-2 text-sm"
        >
          <span class="flex items-center gap-2">
            <History class="h-4 w-4 shrink-0" />
            {{ t('notes.form.draftRestored', { time: formatRelative(restoredDraft.savedAt) }) }}
          </span>
          <Button type="button" size="sm" variant="ghost" @click="discardDraft">
            {{ t('notes.form.discardDraft') }}
          </Button>
        </div>

        <div
          v-if="submitError"
          class="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
        >
          <AlertCircle class="h-4 w-4 shrink-0" />
          <span>{{ submitError }}</span>
        </div>

        <div
          v-if="hasConflict"
          role="alert"
          class="space-y-3 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-700 dark:text-amber-400"
        >
          <div class="flex items-start gap-2">
            <TriangleAlert class="mt-0.5 h-4 w-4 shrink-0" />
            <p>{{ t('notes.form.conflict') }}</p>
          </div>
          <div class="flex flex-wrap gap-2 pl-6">
            <Button type="button" size="sm" variant="outline" :disabled="isResolving" @click="resolveConflict(false)">
              {{ t('notes.form.loadLatest') }}
            </Button>
            <Button type="button" size="sm" :disabled="isResolving" @click="resolveConflict(true)">
              <Loader2 v-if="isResolving" class="h-4 w-4 animate-spin" />
              <span>{{ t('notes.form.overwrite') }}</span>
            </Button>
          </div>
        </div>

        <FormField v-slot="{ componentField }" name="title" :validate-on-model-update="!!errors.title">
          <FormItem>
            <div class="flex items-center justify-between">
              <FormLabel>{{ t('notes.form.title') }}</FormLabel>
              <span class="text-xs tabular-nums text-muted-foreground">
                {{ values.title?.length ?? 0 }}/{{ NOTE_TITLE_MAX_LENGTH }}
              </span>
            </div>
            <FormControl>
              <Input
                v-bind="componentField"
                :maxlength="NOTE_TITLE_MAX_LENGTH"
                :placeholder="t('notes.form.titlePlaceholder')"
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="tags">
          <FormItem>
            <FormLabel>
              {{ t('notes.form.tags') }}
              <span class="font-normal text-muted-foreground">{{ t('notes.form.optional') }}</span>
            </FormLabel>
            <FormControl>
              <TagInput
                v-bind="componentField"
                :suggestions="tagSuggestions"
                :placeholder="t('notes.form.tagsPlaceholder')"
              />
            </FormControl>
            <FormDescription>{{ t('notes.form.tagsHint') }}</FormDescription>
            <FormMessage />
          </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="content">
          <FormItem>
            <div class="flex items-center justify-between gap-2">
              <FormLabel>
                {{ t('notes.form.content') }}
                <span class="font-normal text-muted-foreground">{{ t('notes.form.optional') }}</span>
              </FormLabel>
              <div class="inline-flex rounded-md border p-0.5 text-xs">
                <button
                  type="button"
                  class="rounded px-2.5 py-1 font-medium transition-colors"
                  :class="contentMode === 'write' ? 'bg-secondary text-secondary-foreground' : 'text-muted-foreground hover:text-foreground'"
                  :aria-pressed="contentMode === 'write'"
                  @click="contentMode = 'write'"
                >
                  {{ t('notes.form.write') }}
                </button>
                <button
                  type="button"
                  class="rounded px-2.5 py-1 font-medium transition-colors"
                  :class="contentMode === 'preview' ? 'bg-secondary text-secondary-foreground' : 'text-muted-foreground hover:text-foreground'"
                  :aria-pressed="contentMode === 'preview'"
                  @click="contentMode = 'preview'"
                >
                  {{ t('notes.form.preview') }}
                </button>
              </div>
            </div>
            <FormControl>
              <Textarea
                v-show="contentMode === 'write'"
                v-bind="componentField"
                :placeholder="t('notes.form.contentPlaceholder')"
                class="min-h-48 max-h-[50vh] resize-y"
              />
            </FormControl>
            <div
              v-if="contentMode === 'preview'"
              class="min-h-48 max-h-[50vh] overflow-y-auto rounded-md border bg-muted/30 px-3 py-2"
            >
              <MarkdownContent v-if="values.content?.trim()" :source="values.content" />
              <p v-else class="text-sm italic text-muted-foreground">
                {{ t('notes.form.nothingToPreview') }}
              </p>
            </div>
            <FormDescription>{{ t('notes.form.markdownHint') }}</FormDescription>
          </FormItem>
        </FormField>
      </form>

      <DialogFooter class="sm:items-center">
        <span class="mr-auto hidden text-xs text-muted-foreground sm:block" aria-live="polite">
          <template v-if="draftSavedAt && meta.dirty">{{ t('notes.form.draftSaved') }} · </template>{{ t('notes.form.shortcutHint') }}
        </span>
        <Button type="button" variant="outline" :disabled="isSubmitting" @click="open = false">
          {{ t('common.cancel') }}
        </Button>
        <Button type="submit" form="note-form" :disabled="!canSubmit">
          <Loader2 v-if="isSubmitting" class="h-4 w-4 animate-spin" />
          <span>{{ isEditing ? t('notes.form.saveChanges') : t('notes.form.create') }}</span>
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
