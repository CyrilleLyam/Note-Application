import type { EmptyTrashResult, Note, NotePayload, NoteQuery, UpdateNotePayload } from '../models/note'
import type { BaseApiResponse } from '@/core/models'
import { apiClient } from '@/core/services/apiClient'

export async function getNotes(query?: NoteQuery): Promise<BaseApiResponse<Note[]>> {
  const response = await apiClient.get<BaseApiResponse<Note[]>>('/notes', {
    params: query,
  })
  return response.data
}

export async function getNoteById(id: number): Promise<BaseApiResponse<Note>> {
  const response = await apiClient.get<BaseApiResponse<Note>>(`/notes/${id}`)
  return response.data
}

export async function createNote(payload: NotePayload): Promise<BaseApiResponse<Note>> {
  const response = await apiClient.post<BaseApiResponse<Note>>('/notes', payload)
  return response.data
}

export async function updateNote(id: number, payload: UpdateNotePayload): Promise<BaseApiResponse<Note>> {
  const response = await apiClient.put<BaseApiResponse<Note>>(`/notes/${id}`, payload)
  return response.data
}

export async function setNotePinned(id: number, isPinned: boolean): Promise<BaseApiResponse<Note>> {
  const response = await apiClient.patch<BaseApiResponse<Note>>(`/notes/${id}/pin`, { isPinned })
  return response.data
}

export async function trashNote(id: number): Promise<void> {
  await apiClient.delete(`/notes/${id}`)
}

export async function restoreNote(id: number): Promise<BaseApiResponse<Note>> {
  const response = await apiClient.post<BaseApiResponse<Note>>(`/notes/${id}/restore`)
  return response.data
}

export async function deleteNotePermanently(id: number): Promise<void> {
  await apiClient.delete(`/notes/${id}/permanent`)
}

export async function emptyTrash(): Promise<BaseApiResponse<EmptyTrashResult>> {
  const response = await apiClient.delete<BaseApiResponse<EmptyTrashResult>>('/notes/trash')
  return response.data
}
