import type { SharePermission } from '@/modules/notes/models/note'

export type NotificationType = 'note_shared'

export interface AppNotification {
  id: number
  type: NotificationType
  noteId: number | null
  noteTitle: string | null
  actorName: string | null
  permission: SharePermission | null
  createdAt: string
  readAt: string | null
}

export interface UnreadCount {
  count: number
}

export interface NotificationQuery {
  page?: number
  pageSize?: number
}
