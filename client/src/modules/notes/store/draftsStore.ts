import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface NoteDraft {
  title: string
  content: string
  tags: string[]
  baseVersion: string | null
  savedAt: string
}

function isNoteDraft(value: unknown): value is NoteDraft {
  if (!value || typeof value !== 'object') {
    return false
  }
  const draft = value as Record<string, unknown>
  return typeof draft.title === 'string'
    && typeof draft.content === 'string'
    && Array.isArray(draft.tags)
    && draft.tags.every(tag => typeof tag === 'string')
    && (draft.baseVersion === null || typeof draft.baseVersion === 'string')
    && typeof draft.savedAt === 'string'
}

export const useDraftsStore = defineStore('drafts', () => {
  const drafts = ref<Record<string, NoteDraft>>({})

  function draftKey(userId: number, noteId?: number | null) {
    return noteId ? `${userId}:note-${noteId}` : `${userId}:new`
  }

  function getDraft(key: string): NoteDraft | null {
    const draft = drafts.value[key]
    return isNoteDraft(draft) ? draft : null
  }

  function saveDraft(key: string, draft: Omit<NoteDraft, 'savedAt'>) {
    drafts.value = { ...drafts.value, [key]: { ...draft, savedAt: new Date().toISOString() } }
  }

  function clearDraft(key: string) {
    if (key in drafts.value) {
      drafts.value = Object.fromEntries(Object.entries(drafts.value).filter(([entryKey]) => entryKey !== key))
    }
  }

  function clearAll() {
    drafts.value = {}
  }

  return { drafts, draftKey, getDraft, saveDraft, clearDraft, clearAll }
}, {
  persist: {
    key: 'note-drafts',
    pick: ['drafts'],
    afterHydrate: ({ store }) => {
      if (!store.drafts || typeof store.drafts !== 'object' || Array.isArray(store.drafts)) {
        store.drafts = {}
      }
    },
  },
})
