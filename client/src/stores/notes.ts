import type { Note, NotePayload, NoteQuery, NoteSortBy, PaginationMeta, SortOrder, Tag, UpdateNotePayload } from '@/types'
import { refDebounced } from '@vueuse/core'
import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import { describeError } from '@/lib/api/errors'
import {
  createNote as createNoteRequest,
  deleteNotePermanently,
  emptyTrash as emptyTrashRequest,
  getNoteById,
  getNotes,
  restoreNote as restoreNoteRequest,
  setNotePinned,
  trashNote as trashNoteRequest,
  updateNote as updateNoteRequest,
} from '@/lib/api/notes'
import { getTags } from '@/lib/api/tags'
import { startOfDayOffset } from '@/lib/date'

export const DATE_FILTERS = {
  all: { labelKey: 'notes.dateFilters.all', daysAgo: null },
  today: { labelKey: 'notes.dateFilters.today', daysAgo: 0 },
  week: { labelKey: 'notes.dateFilters.week', daysAgo: 6 },
  month: { labelKey: 'notes.dateFilters.month', daysAgo: 29 },
} as const satisfies Record<string, { labelKey: string, daysAgo: number | null }>

export const SORT_OPTIONS = {
  newest: { labelKey: 'notes.sortOptions.newest', sortBy: 'created_at', sortOrder: 'desc' },
  oldest: { labelKey: 'notes.sortOptions.oldest', sortBy: 'created_at', sortOrder: 'asc' },
  updated: { labelKey: 'notes.sortOptions.updated', sortBy: 'updated_at', sortOrder: 'desc' },
  titleAsc: { labelKey: 'notes.sortOptions.titleAsc', sortBy: 'title', sortOrder: 'asc' },
  titleDesc: { labelKey: 'notes.sortOptions.titleDesc', sortBy: 'title', sortOrder: 'desc' },
} as const satisfies Record<string, { labelKey: string, sortBy: NoteSortBy, sortOrder: SortOrder }>

export type DateFilter = keyof typeof DATE_FILTERS
export type SortOption = keyof typeof SORT_OPTIONS
export type NotesView = 'active' | 'trash'

const DEFAULT_DATE_FILTER: DateFilter = 'all'
const DEFAULT_SORT: SortOption = 'newest'
const DEFAULT_PAGE_SIZE = 12

export const useNotesStore = defineStore('notes', () => {
  const notes = ref<Note[]>([])
  const meta = ref<PaginationMeta | null>(null)
  const isLoading = ref(false)
  const hasLoaded = ref(false)
  const error = ref<string | null>(null)

  const currentNote = ref<Note | null>(null)
  const isLoadingNote = ref(false)
  const noteError = ref<string | null>(null)

  const tags = ref<Tag[]>([])
  const isDeleting = ref(false)

  const view = ref<NotesView>('active')
  const search = ref('')
  const tag = ref<string | null>(null)
  const dateFilter = ref<DateFilter>(DEFAULT_DATE_FILTER)
  const sort = ref<SortOption>(DEFAULT_SORT)
  const page = ref(1)
  const pageSize = ref(DEFAULT_PAGE_SIZE)

  const debouncedSearch = refDebounced(search, 300)

  const isTrashView = computed(() => view.value === 'trash')

  const hasActiveFilters = computed(() => debouncedSearch.value.trim() !== ''
    || tag.value !== null
    || dateFilter.value !== DEFAULT_DATE_FILTER)

  const query = computed<NoteQuery>(() => {
    const { daysAgo } = DATE_FILTERS[dateFilter.value]
    const { sortBy, sortOrder } = SORT_OPTIONS[sort.value]

    return {
      page: page.value,
      pageSize: pageSize.value,
      search: debouncedSearch.value.trim() || undefined,
      tag: tag.value ?? undefined,
      trashed: isTrashView.value || undefined,
      createdFrom: daysAgo === null ? undefined : startOfDayOffset(daysAgo),
      sortBy,
      sortOrder,
    }
  })

  watch([debouncedSearch, tag, dateFilter, sort, view], () => {
    page.value = 1
  })

  let listRequestId = 0
  let noteRequestId = 0

  async function fetchNotes() {
    const requestId = ++listRequestId
    isLoading.value = true
    error.value = null

    try {
      const response = await getNotes(query.value)
      if (requestId !== listRequestId) {
        return
      }

      notes.value = response.data
      meta.value = response.meta ?? null
      hasLoaded.value = true

      if (meta.value && meta.value.totalPages > 0 && meta.value.page > meta.value.totalPages) {
        page.value = meta.value.totalPages
      }
    }
    catch (err) {
      if (requestId === listRequestId) {
        error.value = describeError(err)
      }
    }
    finally {
      if (requestId === listRequestId) {
        isLoading.value = false
      }
    }
  }

  async function fetchTags() {
    try {
      const response = await getTags()
      tags.value = response.data
      if (tag.value !== null && !tags.value.some(item => item.name === tag.value)) {
        tag.value = null
      }
    }
    catch {
      tags.value = []
    }
  }

  function refreshLists() {
    void fetchNotes()
    void fetchTags()
  }

  async function fetchNote(id: number) {
    const requestId = ++noteRequestId
    currentNote.value = notes.value.find(note => note.id === id) ?? null
    isLoadingNote.value = true
    noteError.value = null

    try {
      const response = await getNoteById(id)
      if (requestId === noteRequestId) {
        currentNote.value = response.data
      }
    }
    catch (err) {
      if (requestId === noteRequestId) {
        currentNote.value = null
        noteError.value = describeError(err)
      }
    }
    finally {
      if (requestId === noteRequestId) {
        isLoadingNote.value = false
      }
    }
  }

  function replaceNote(updated: Note) {
    notes.value = notes.value.map(note => note.id === updated.id ? updated : note)
    if (currentNote.value?.id === updated.id) {
      currentNote.value = updated
    }
  }

  function removeNote(id: number) {
    notes.value = notes.value.filter(note => note.id !== id)
  }

  async function createNote(payload: NotePayload) {
    const response = await createNoteRequest(payload)
    refreshLists()
    return response.data
  }

  async function updateNote(id: number, payload: UpdateNotePayload) {
    const response = await updateNoteRequest(id, payload)
    replaceNote(response.data)
    refreshLists()
    return response.data
  }

  async function refreshNote(id: number) {
    const response = await getNoteById(id)
    replaceNote(response.data)
    return response.data
  }

  async function togglePin(note: Note) {
    const response = await setNotePinned(note.id, !note.isPinned)
    replaceNote(response.data)
    void fetchNotes()
    return response.data
  }

  async function trashNote(id: number) {
    await trashNoteRequest(id)
    removeNote(id)
    if (currentNote.value?.id === id) {
      currentNote.value = null
    }
    refreshLists()
  }

  async function restoreNote(id: number) {
    const response = await restoreNoteRequest(id)
    removeNote(id)
    refreshLists()
    return response.data
  }

  async function deleteForever(id: number) {
    isDeleting.value = true
    try {
      await deleteNotePermanently(id)
      removeNote(id)
      if (currentNote.value?.id === id) {
        currentNote.value = null
      }
      void fetchNotes()
    }
    finally {
      isDeleting.value = false
    }
  }

  async function emptyTrash() {
    isDeleting.value = true
    try {
      const response = await emptyTrashRequest()
      notes.value = []
      void fetchNotes()
      return response.data.deletedCount
    }
    finally {
      isDeleting.value = false
    }
  }

  function setView(value: NotesView) {
    view.value = value
  }

  function clearCurrentNote() {
    noteRequestId++
    currentNote.value = null
    isLoadingNote.value = false
    noteError.value = null
  }

  function resetFilters() {
    search.value = ''
    tag.value = null
    dateFilter.value = DEFAULT_DATE_FILTER
    sort.value = DEFAULT_SORT
    page.value = 1
  }

  function reset() {
    listRequestId++
    notes.value = []
    meta.value = null
    tags.value = []
    isLoading.value = false
    hasLoaded.value = false
    error.value = null
    view.value = 'active'
    clearCurrentNote()
    resetFilters()
  }

  return {
    notes,
    meta,
    isLoading,
    hasLoaded,
    error,
    currentNote,
    isLoadingNote,
    noteError,
    tags,
    isDeleting,
    view,
    isTrashView,
    search,
    tag,
    dateFilter,
    sort,
    page,
    pageSize,
    query,
    hasActiveFilters,
    fetchNotes,
    fetchTags,
    fetchNote,
    createNote,
    updateNote,
    refreshNote,
    togglePin,
    trashNote,
    restoreNote,
    deleteForever,
    emptyTrash,
    setView,
    clearCurrentNote,
    resetFilters,
    reset,
  }
}, {
  persist: {
    key: 'notes-preferences',
    pick: ['dateFilter', 'sort'],
    afterHydrate: ({ store }) => {
      if (!Object.keys(DATE_FILTERS).includes(store.dateFilter)) {
        store.dateFilter = DEFAULT_DATE_FILTER
      }
      if (!Object.keys(SORT_OPTIONS).includes(store.sort)) {
        store.sort = DEFAULT_SORT
      }
    },
  },
})
