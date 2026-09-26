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
