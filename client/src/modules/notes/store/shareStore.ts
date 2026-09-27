import type { NoteShare, SharedNote, SharePermission } from '../models/note'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { describeError, isApiError } from '@/core/services/apiError'
import {
  addNoteShare,
  createShareLink as createShareLinkRequest,
  getNoteShares,
  getSharedNote,
  getShareLink,
  removeNoteShare,
  revokeShareLink as revokeShareLinkRequest,
  updateNoteShare,
} from '../services/shareService'

export const useShareStore = defineStore('share', () => {
  const shareToken = ref<string | null>(null)
  const isLoadingLink = ref(false)
  const isSavingLink = ref(false)
  const linkError = ref<string | null>(null)

  const sharedNote = ref<SharedNote | null>(null)
  const isLoadingSharedNote = ref(false)
  const sharedNoteError = ref<string | null>(null)
  const isSharedNoteMissing = ref(false)

  const shares = ref<NoteShare[]>([])
  const isLoadingShares = ref(false)
  const isSavingShare = ref(false)
  const sharesError = ref<string | null>(null)

  let linkRequestId = 0
  let sharedNoteRequestId = 0
  let sharesRequestId = 0

  async function fetchShareLink(noteId: number) {
    const requestId = ++linkRequestId
    shareToken.value = null
    isLoadingLink.value = true
    linkError.value = null

    try {
      const response = await getShareLink(noteId)
      if (requestId === linkRequestId) {
        shareToken.value = response.data.token
      }
    }
    catch (err) {
      if (requestId === linkRequestId) {
        linkError.value = describeError(err)
      }
    }
    finally {
      if (requestId === linkRequestId) {
        isLoadingLink.value = false
      }
    }
  }

  async function createShareLink(noteId: number) {
    isSavingLink.value = true
    try {
      const response = await createShareLinkRequest(noteId)
      shareToken.value = response.data.token
    }
    finally {
      isSavingLink.value = false
    }
  }

  async function revokeShareLink(noteId: number) {
    isSavingLink.value = true
    try {
      await revokeShareLinkRequest(noteId)
      shareToken.value = null
    }
    finally {
      isSavingLink.value = false
    }
  }

  async function fetchShares(noteId: number) {
    const requestId = ++sharesRequestId
    shares.value = []
    isLoadingShares.value = true
    sharesError.value = null

    try {
      const response = await getNoteShares(noteId)
      if (requestId === sharesRequestId) {
        shares.value = response.data
      }
    }
    catch (err) {
      if (requestId === sharesRequestId) {
        sharesError.value = describeError(err)
      }
    }
    finally {
      if (requestId === sharesRequestId) {
        isLoadingShares.value = false
      }
    }
  }

  async function addShare(noteId: number, email: string, permission: SharePermission) {
    isSavingShare.value = true
    try {
      const response = await addNoteShare(noteId, { email, permission })
      const share = response.data
      shares.value = [...shares.value.filter(item => item.userId !== share.userId), share]
      return share
    }
    finally {
      isSavingShare.value = false
    }
  }

  async function updateShare(noteId: number, userId: number, permission: SharePermission) {
    const response = await updateNoteShare(noteId, userId, permission)
    shares.value = shares.value.map(item => item.userId === userId ? response.data : item)
  }

  async function removeShare(noteId: number, userId: number) {
    await removeNoteShare(noteId, userId)
    shares.value = shares.value.filter(item => item.userId !== userId)
  }

  async function fetchSharedNote(token: string) {
    const requestId = ++sharedNoteRequestId
    sharedNote.value = null
    isLoadingSharedNote.value = true
    sharedNoteError.value = null
    isSharedNoteMissing.value = false

    try {
      const response = await getSharedNote(token)
      if (requestId === sharedNoteRequestId) {
        sharedNote.value = response.data
      }
    }
    catch (err) {
      if (requestId === sharedNoteRequestId) {
        isSharedNoteMissing.value = isApiError(err, 404)
        sharedNoteError.value = describeError(err)
      }
    }
    finally {
      if (requestId === sharedNoteRequestId) {
        isLoadingSharedNote.value = false
      }
    }
  }

  return {
    shareToken,
    isLoadingLink,
    isSavingLink,
    linkError,
    sharedNote,
    isLoadingSharedNote,
    sharedNoteError,
    isSharedNoteMissing,
    shares,
    isLoadingShares,
    isSavingShare,
    sharesError,
    fetchShareLink,
    createShareLink,
    revokeShareLink,
    fetchShares,
    addShare,
    updateShare,
    removeShare,
    fetchSharedNote,
  }
})
