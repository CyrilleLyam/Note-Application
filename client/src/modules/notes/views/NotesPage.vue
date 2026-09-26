<script setup lang="ts">
import type { Note } from '../models/note'
import type { NotesView } from '../store/notesStore'
import { AlertCircle, Keyboard, NotebookText, Plus, RotateCcw, SearchX, Trash2 } from '@lucide/vue'
import { storeToRefs } from 'pinia'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from 'vue-sonner'
import { Button } from '@/core/components/ui/button'
import { Card } from '@/core/components/ui/card'
import { Skeleton } from '@/core/components/ui/skeleton'
import { describeError } from '@/core/services/apiError'
import { useAuthStore } from '@/modules/auth'
import DeleteNoteDialog from '../components/DeleteNoteDialog.vue'
import NoteCard from '../components/NoteCard.vue'
import NoteDetailDialog from '../components/NoteDetailDialog.vue'
import NoteFormDialog from '../components/NoteFormDialog.vue'
import NotesPagination from '../components/NotesPagination.vue'
import NotesToolbar from '../components/NotesToolbar.vue'
import ShortcutsDialog from '../components/ShortcutsDialog.vue'
import { useKeyboardShortcuts } from '../composables/useKeyboardShortcuts'
import { useNotesStore } from '../store/notesStore'

const { t } = useI18n()
const auth = useAuthStore()
const notesStore = useNotesStore()
const { notes, meta, isLoading, hasLoaded, error, page, query, hasActiveFilters, view, isTrashView } = storeToRefs(notesStore)

const toolbar = ref<InstanceType<typeof NotesToolbar> | null>(null)
const isFormOpen = ref(false)
const editingNote = ref<Note | null>(null)
const isDetailOpen = ref(false)
const selectedNoteId = ref<number | null>(null)
const isDeleteOpen = ref(false)
const deleteMode = ref<'note' | 'trash'>('note')
const deletingNote = ref<Note | null>(null)
const isShortcutsOpen = ref(false)

const views: Array<{ value: NotesView, labelKey: 'notes.viewNotes' | 'notes.viewTrash' }> = [
  { value: 'active', labelKey: 'notes.viewNotes' },
  { value: 'trash', labelKey: 'notes.viewTrash' },
]

const summary = computed(() => {
  if (!meta.value) {
    return isTrashView.value ? t('notes.trashSummary') : t('notes.summaryEmpty')
  }
  const count = meta.value.totalCount
  if (hasActiveFilters.value) {
    return t('notes.countMatching', count)
  }
  return isTrashView.value ? `${t('notes.count', count)} · ${t('notes.trashSummary')}` : t('notes.count', count)
})

watch(query, () => {
  if (auth.isAuthenticated) {
    notesStore.fetchNotes()
  }
}, { immediate: true })

onMounted(() => {
  if (auth.isAuthenticated) {
    notesStore.fetchTags()
  }
})

useKeyboardShortcuts({
  'n': () => {
    if (!isTrashView.value) {
      openCreate()
    }
  },
  '/': () => toolbar.value?.focusSearch(),
  '?': () => {
    isShortcutsOpen.value = true
  },
})

function openCreate() {
  editingNote.value = null
  isFormOpen.value = true
}

function openDetail(note: Note) {
  selectedNoteId.value = note.id
  isDetailOpen.value = true
}

function openEdit(note: Note) {
  isDetailOpen.value = false
  editingNote.value = note
  isFormOpen.value = true
}

function closeDetailFor(id: number) {
  if (selectedNoteId.value === id) {
    isDetailOpen.value = false
    selectedNoteId.value = null
    notesStore.clearCurrentNote()
  }
}

async function handleTogglePin(note: Note) {
  try {
    const updated = await notesStore.togglePin(note)
    toast.success(updated.isPinned ? t('notes.toasts.pinned') : t('notes.toasts.unpinned'))
  }
  catch (err) {
    toast.error(describeError(err))
  }
}

async function handleRestore(note: Note, showToast = true) {
  try {
    await notesStore.restoreNote(note.id)
    closeDetailFor(note.id)
    if (showToast) {
      toast.success(t('notes.toasts.restored'))
    }
  }
  catch (err) {
    toast.error(describeError(err))
  }
}

async function handleTrash(note: Note) {
  try {
    await notesStore.trashNote(note.id)
    closeDetailFor(note.id)
    toast.success(t('notes.toasts.movedToTrash'), {
      duration: 8000,
      action: {
        label: t('common.undo'),
        onClick: () => handleRestore(note, false),
      },
    })
  }
  catch (err) {
    toast.error(describeError(err))
  }
}

function openDeleteForever(note: Note) {
  deleteMode.value = 'note'
  deletingNote.value = note
  isDeleteOpen.value = true
}

function openEmptyTrash() {
  deleteMode.value = 'trash'
  deletingNote.value = null
  isDeleteOpen.value = true
}

function handleDeleted(id: number | null) {
  if (id === null) {
    isDetailOpen.value = false
    return
  }
  closeDetailFor(id)
}
</script>

<template>
  <AppLayout>
    <div class="space-y-6">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div class="space-y-1">
          <h1 class="text-2xl font-bold tracking-tight sm:text-3xl">
            {{ isTrashView ? t('notes.trashTitle') : t('notes.pageTitle') }}
          </h1>
          <p class="text-sm text-muted-foreground" aria-live="polite">
            {{ summary }}
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <div class="inline-flex rounded-md border p-0.5" role="group" :aria-label="t('notes.viewSwitcher')">
            <button
              v-for="item in views"
              :key="item.value"
              type="button"
              class="rounded px-3 py-1.5 text-sm font-medium transition-colors"
              :class="view === item.value ? 'bg-secondary text-secondary-foreground shadow-xs' : 'text-muted-foreground hover:text-foreground'"
              :aria-pressed="view === item.value"
              @click="notesStore.setView(item.value)"
            >
              {{ t(item.labelKey) }}
            </button>
          </div>
          <Button variant="ghost" size="icon" :aria-label="t('shortcuts.title')" aria-keyshortcuts="?" @click="isShortcutsOpen = true">
            <Keyboard class="h-4 w-4" />
          </Button>
          <Button
            v-if="isTrashView"
            variant="outline"
            class="text-destructive hover:text-destructive"
            :disabled="!notes.length"
            @click="openEmptyTrash"
          >
            <Trash2 class="h-4 w-4" />
            <span>{{ t('notes.emptyTrash') }}</span>
          </Button>
          <Button v-else aria-keyshortcuts="n" @click="openCreate">
            <Plus class="h-4 w-4" />
            <span>{{ t('notes.newNote') }}</span>
          </Button>
        </div>
      </div>

      <NotesToolbar ref="toolbar" />

      <div v-if="!hasLoaded && isLoading" class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Card v-for="n in 6" :key="n" class="gap-4 p-5">
          <Skeleton class="h-5 w-3/4" />
          <div class="space-y-2">
            <Skeleton class="h-4 w-full" />
            <Skeleton class="h-4 w-5/6" />
            <Skeleton class="h-4 w-2/3" />
          </div>
          <Skeleton class="h-4 w-24" />
        </Card>
      </div>

      <div
        v-else-if="error && !notes.length"
        class="flex flex-col gap-3 rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive sm:flex-row sm:items-center sm:justify-between"
      >
        <div class="flex items-center gap-2">
          <AlertCircle class="h-5 w-5 shrink-0" />
          <span>{{ error }}</span>
        </div>
        <Button variant="outline" size="sm" @click="notesStore.fetchNotes()">
          {{ t('common.retry') }}
        </Button>
      </div>

      <div v-else-if="notes.length" class="space-y-6">
        <ul
          class="grid grid-cols-1 gap-4 transition-opacity sm:grid-cols-2 lg:grid-cols-3"
          :class="{ 'opacity-60': isLoading }"
          :aria-busy="isLoading"
        >
          <li v-for="note in notes" :key="note.id" class="flex">
            <NoteCard
              :note="note"
              class="w-full"
              @view="openDetail"
              @edit="openEdit"
              @toggle-pin="handleTogglePin"
              @trash="handleTrash"
              @restore="handleRestore"
              @delete-forever="openDeleteForever"
            />
          </li>
        </ul>

        <NotesPagination
          v-if="meta && meta.totalPages > 1"
          v-model:page="page"
          :meta="meta"
        />
      </div>

      <Card v-else-if="hasActiveFilters" class="items-center gap-4 p-12 text-center">
        <div class="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
          <SearchX class="h-6 w-6" />
        </div>
        <div class="space-y-1">
          <h2 class="font-semibold">
            {{ t('notes.noMatchesTitle') }}
          </h2>
          <p class="text-sm text-muted-foreground">
            {{ t('notes.noMatchesDescription') }}
          </p>
        </div>
        <Button variant="outline" @click="notesStore.resetFilters()">
          <RotateCcw class="h-4 w-4" />
          <span>{{ t('notes.resetFilters') }}</span>
        </Button>
      </Card>

      <Card v-else-if="isTrashView" class="items-center gap-4 p-12 text-center">
        <div class="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
          <Trash2 class="h-6 w-6" />
        </div>
        <div class="space-y-1">
          <h2 class="font-semibold">
            {{ t('notes.trashEmptyTitle') }}
          </h2>
          <p class="text-sm text-muted-foreground">
            {{ t('notes.trashEmptyDescription') }}
          </p>
        </div>
      </Card>

      <Card v-else class="items-center gap-4 p-12 text-center">
        <div class="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
          <NotebookText class="h-6 w-6" />
        </div>
        <div class="space-y-1">
          <h2 class="font-semibold">
            {{ t('notes.emptyTitle') }}
          </h2>
          <p class="text-sm text-muted-foreground">
            {{ t('notes.emptyDescription') }}
          </p>
        </div>
        <Button @click="openCreate">
          <Plus class="h-4 w-4" />
          <span>{{ t('notes.newNote') }}</span>
        </Button>
      </Card>
    </div>

    <NoteFormDialog v-model:open="isFormOpen" :note="editingNote" />
    <NoteDetailDialog
      v-model:open="isDetailOpen"
      :note-id="selectedNoteId"
      @edit="openEdit"
      @toggle-pin="handleTogglePin"
      @trash="handleTrash"
      @restore="handleRestore"
      @delete-forever="openDeleteForever"
    />
    <DeleteNoteDialog
      v-model:open="isDeleteOpen"
      :mode="deleteMode"
      :note="deletingNote"
      @deleted="handleDeleted"
    />
    <ShortcutsDialog v-model:open="isShortcutsOpen" />
  </AppLayout>
</template>
