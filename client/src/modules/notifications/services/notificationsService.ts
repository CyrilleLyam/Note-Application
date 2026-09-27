import type { AppNotification, NotificationQuery, UnreadCount } from '../models/notification'
import type { BaseApiResponse } from '@/core/models'
import { apiClient } from '@/core/services/apiClient'
import { ApiError } from '@/core/services/apiError'
import { getAccessToken } from '@/core/utils/cookies'
import { CaseConverter } from '@/core/utils/utils'

export async function getNotifications(query?: NotificationQuery): Promise<BaseApiResponse<AppNotification[]>> {
  const response = await apiClient.get<BaseApiResponse<AppNotification[]>>('/notifications', { params: query })
  return response.data
}

export async function getUnreadCount(): Promise<BaseApiResponse<UnreadCount>> {
  const response = await apiClient.get<BaseApiResponse<UnreadCount>>('/notifications/unread-count')
  return response.data
}

export async function markNotificationRead(id: number): Promise<void> {
  await apiClient.post(`/notifications/${id}/read`)
}

export async function markAllNotificationsRead(): Promise<void> {
  await apiClient.post('/notifications/read-all')
}

interface StreamHandlers {
  signal: AbortSignal
  onOpen: () => void
  onNotification: (notification: AppNotification) => void
}

export async function streamNotifications({ signal, onOpen, onNotification }: StreamHandlers): Promise<void> {
  const token = getAccessToken()
  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/notifications/stream`, {
    headers: {
      Accept: 'text/event-stream',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    signal,
  })

  if (!response.ok || !response.body) {
    throw new ApiError(response.statusText, response.status)
  }

  onOpen()

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { value, done } = await reader.read()
    if (done) {
      return
    }

    buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n')

    let boundary = buffer.indexOf('\n\n')
    while (boundary >= 0) {
      const block = buffer.slice(0, boundary)
      buffer = buffer.slice(boundary + 2)
      boundary = buffer.indexOf('\n\n')

      let event = 'message'
      const data: string[] = []
      for (const line of block.split('\n')) {
        if (line.startsWith('event:')) {
          event = line.slice(6).trim()
        }
        else if (line.startsWith('data:')) {
          data.push(line.slice(5).trimStart())
        }
      }

      if (event === 'notification' && data.length) {
        onNotification(CaseConverter.toCamelCase(JSON.parse(data.join('\n'))) as AppNotification)
      }
    }
  }
}
