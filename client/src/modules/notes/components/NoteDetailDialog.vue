<script setup lang="ts">
import type { Note } from '../models/note'
import { AlertCircle, ArchiveRestore, Calendar, CalendarClock, Pencil, Pin, PinOff, Trash2 } from '@lucide/vue'
import { storeToRefs } from 'pinia'
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Badge } from '@/core/components/ui/badge'
import { Button } from '@/core/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/core/components/ui/dialog'
import { Skeleton } from '@/core/components/ui/skeleton'
import { formatDateTime } from '@/core/utils/date'
import { useNotesStore } from '../store/notesStore'
import MarkdownContent from './MarkdownContent.vue'

const props = defineProps<{
  noteId: number | null
}>()

const emit = defineEmits<{
  edit: [note: Note]
  togglePin: [note: Note]
  trash: [note: Note]
  restore: [note: Note]
  deleteForever: [note: Note]
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const notesStore = useNotesStore()
const { currentNote: note, noteError: error } = storeToRefs(notesStore)

const isTrashed = computed(() => !!note.value?.deletedAt)

function loadNote() {
  if (props.noteId !== null) {
    notesStore.fetchNote(props.noteId)
  }
}

watch([open, () => props.noteId], ([isOpen]) => {
  if (isOpen) {
    loadNote()
  }
}, { immediate: true })
</script>

<template>
  <Dialog v-model:open="open">
    <DialogContent class="flex max-h-[calc(100dvh-2rem)] flex-col sm:max-w-2xl">
      <template v-if="note">
        <DialogHeader class="pr-8">
          <DialogTitle class="flex items-start gap-2 break-words text-xl leading-snug">
            <Pin v-if="note.isPinned && !isTrashed" class="mt-1 h-4 w-4 shrink-0 fill-current text-primary" :aria-label="t('notes.pinned')" />
            <span>{{ note.title }}</span>
          </DialogTitle>
          <DialogDescription class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs">
            <span class="flex items-center gap-1.5">
              <Calendar class="h-3.5 w-3.5" />
              {{ t('notes.createdAt', { date: formatDateTime(note.createdAt) }) }}
            </span>
            <span v-if="note.updatedAt" class="flex items-center gap-1.5">
              <CalendarClock class="h-3.5 w-3.5" />
              {{ t('notes.updatedAt', { date: formatDateTime(note.updatedAt) }) }}
            </span>
            <span v-if="note.deletedAt" class="flex items-center gap-1.5 text-destructive">
              <Trash2 class="h-3.5 w-3.5" />
              {{ t('notes.deletedAt', { date: formatDateTime(note.deletedAt) }) }}
            </span>
          </DialogDescription>
        </DialogHeader>

        <ul v-if="note.tags.length" class="flex flex-wrap gap-1.5">
          <li v-for="tag in note.tags" :key="tag">
            <Badge variant="outline" class="font-normal">
              {{ tag }}
            </Badge>
          </li>
        </ul>

        <div class="min-h-24 overflow-y-auto rounded-md border bg-muted/30 p-4">
          <MarkdownContent v-if="note.content" :source="note.content" />
          <p v-else class="text-sm italic text-muted-foreground">
            {{ t('notes.detail.noContent') }}
          </p>
        </div>

        <DialogFooter class="gap-2 sm:justify-between">
          <template v-if="isTrashed">
            <Button variant="outline" class="text-destructive hover:text-destructive" @click="emit('deleteForever', note)">
              <Trash2 class="h-4 w-4" />
              <span>{{ t('notes.actions.deleteForever') }}</span>
            </Button>
            <Button @click="emit('restore', note)">
              <ArchiveRestore class="h-4 w-4" />
              <span>{{ t('notes.actions.restore') }}</span>
            </Button>
          </template>
          <template v-else>
            <Button variant="outline" class="text-destructive hover:text-destructive" @click="emit('trash', note)">
              <Trash2 class="h-4 w-4" />
              <span>{{ t('notes.actions.moveToTrash') }}</span>
            </Button>
            <div class="flex flex-col-reverse gap-2 sm:flex-row">
              <Button variant="outline" :aria-pressed="note.isPinned" @click="emit('togglePin', note)">
                <PinOff v-if="note.isPinned" class="h-4 w-4" />
                <Pin v-else class="h-4 w-4" />
                <span>{{ note.isPinned ? t('notes.actions.unpin') : t('notes.actions.pin') }}</span>
              </Button>
              <Button @click="emit('edit', note)">
                <Pencil class="h-4 w-4" />
                <span>{{ t('notes.actions.edit') }}</span>
              </Button>
            </div>
          </template>
        </DialogFooter>
      </template>

      <template v-else-if="error">
        <DialogHeader>
          <DialogTitle class="flex items-center gap-2">
            <AlertCircle class="h-5 w-5 text-destructive" />
            {{ t('notes.detail.loadError') }}
          </DialogTitle>
          <DialogDescription>{{ error }}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" @click="loadNote">
            {{ t('common.retry') }}
          </Button>
        </DialogFooter>
      </template>

      <template v-else>
        <DialogHeader>
          <DialogTitle class="sr-only">
            {{ t('notes.detail.loading') }}
          </DialogTitle>
          <DialogDescription class="sr-only">
            {{ t('notes.detail.loadingDescription') }}
          </DialogDescription>
          <Skeleton class="h-7 w-2/3" />
          <Skeleton class="h-4 w-1/3" />
        </DialogHeader>
        <Skeleton class="h-40 w-full" />
      </template>
    </DialogContent>
  </Dialog>
</template>
