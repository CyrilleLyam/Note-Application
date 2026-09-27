export type NotePermission = 'owner' | 'edit' | 'view'

export type SharePermission = Exclude<NotePermission, 'owner'>

export interface Note {
  id: number
  title: string
  content: string | null
  isPinned: boolean
  tags: string[]
  createdAt: string
  updatedAt: string | null
  deletedAt: string | null
  rowVersion: string
  permission: NotePermission
  ownerName: string | null
}

export interface Tag {
  name: string
  noteCount: number
}

export type NoteSortBy = 'title' | 'created_at' | 'updated_at'

export type SortOrder = 'asc' | 'desc'

export interface NoteQuery {
  page?: number
  pageSize?: number
  search?: string
  createdFrom?: string
  createdTo?: string
  sortBy?: NoteSortBy
  sortOrder?: SortOrder
  tag?: string
  trashed?: boolean
  shared?: boolean
}

export interface NotePayload {
  title: string
  content?: string | null
  tags: string[]
}

export interface UpdateNotePayload extends NotePayload {
  rowVersion: string
}

export interface EmptyTrashResult {
  deletedCount: number
}

export interface ShareLink {
  token: string | null
}

export interface NoteShare {
  userId: number
  username: string
  displayName: string | null
  email: string
  permission: SharePermission
  createdAt: string
}

export interface NoteSharePayload {
  email: string
  permission: SharePermission
}

export interface SharedNote {
  title: string
  content: string | null
  author: string
  createdAt: string
  updatedAt: string | null
}
