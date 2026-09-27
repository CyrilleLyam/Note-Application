import type { NoteShare, NoteSharePayload, SharedNote, ShareLink, SharePermission } from '../models/note'
import type { BaseApiResponse } from '@/core/models'
import { apiClient } from '@/core/services/apiClient'

export async function getShareLink(noteId: number): Promise<BaseApiResponse<ShareLink>> {
  const response = await apiClient.get<BaseApiResponse<ShareLink>>(`/notes/${noteId}/share`)
  return response.data
}

export async function createShareLink(noteId: number): Promise<BaseApiResponse<ShareLink>> {
  const response = await apiClient.post<BaseApiResponse<ShareLink>>(`/notes/${noteId}/share`)
  return response.data
}

export async function revokeShareLink(noteId: number): Promise<void> {
  await apiClient.delete(`/notes/${noteId}/share`)
}

export async function getNoteShares(noteId: number): Promise<BaseApiResponse<NoteShare[]>> {
  const response = await apiClient.get<BaseApiResponse<NoteShare[]>>(`/notes/${noteId}/shares`)
  return response.data
}

export async function addNoteShare(noteId: number, payload: NoteSharePayload): Promise<BaseApiResponse<NoteShare>> {
  const response = await apiClient.post<BaseApiResponse<NoteShare>>(`/notes/${noteId}/shares`, payload)
  return response.data
}

export async function updateNoteShare(noteId: number, userId: number, permission: SharePermission): Promise<BaseApiResponse<NoteShare>> {
  const response = await apiClient.patch<BaseApiResponse<NoteShare>>(`/notes/${noteId}/shares/${userId}`, { permission })
  return response.data
}

export async function removeNoteShare(noteId: number, userId: number): Promise<void> {
  await apiClient.delete(`/notes/${noteId}/shares/${userId}`)
}

export async function getSharedNote(token: string): Promise<BaseApiResponse<SharedNote>> {
  const response = await apiClient.get<BaseApiResponse<SharedNote>>(`/shared/${encodeURIComponent(token)}`)
  return response.data
}
