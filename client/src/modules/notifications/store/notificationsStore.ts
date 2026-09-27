import type { AppNotification } from '../models/notification'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { toast } from 'vue-sonner'
import { describeError } from '@/core/services/apiError'
import { i18n } from '@/plugins/i18n'
import router from '@/router'
import {
  getNotifications,
  getUnreadCount,
  markAllNotificationsRead,
  markNotificationRead,
  streamNotifications,
} from '../services/notificationsService'

const PAGE_SIZE = 20
const MAX_RECONNECT_DELAY_MS = 30000

export const useNotificationsStore = defineStore('notifications', () => {
  const notifications = ref<AppNotification[]>([])
  const unreadCount = ref(0)
  const isLoading = ref(false)
  const hasLoaded = ref(false)
  const error = ref<string | null>(null)
  const isConnected = ref(false)

  let listRequestId = 0
  let isStreaming = false
  let streamController: AbortController | null = null

  async function fetchNotifications() {
    const requestId = ++listRequestId
    isLoading.value = true
    error.value = null

    try {
      const response = await getNotifications({ page: 1, pageSize: PAGE_SIZE })
      if (requestId === listRequestId) {
        notifications.value = response.data
        hasLoaded.value = true
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

  async function fetchUnreadCount() {
    const response = await getUnreadCount()
    unreadCount.value = response.data.count
  }

  async function markRead(notification: AppNotification) {
    if (notification.readAt) {
      return
    }
    const readAt = new Date().toISOString()
    notifications.value = notifications.value.map(item => item.id === notification.id ? { ...item, readAt } : item)
    unreadCount.value = Math.max(0, unreadCount.value - 1)
    try {
      await markNotificationRead(notification.id)
    }
    catch {
      void fetchUnreadCount()
    }
  }

  async function markAllRead() {
    const readAt = new Date().toISOString()
    notifications.value = notifications.value.map(item => item.readAt ? item : { ...item, readAt })
    unreadCount.value = 0
    try {
      await markAllNotificationsRead()
    }
    catch (err) {
      toast.error(describeError(err))
      void fetchNotifications()
      void fetchUnreadCount()
    }
  }

  function open(notification: AppNotification) {
    void markRead(notification)
    if (notification.noteId !== null) {
      router.push({ name: 'home', query: { view: 'shared', note: String(notification.noteId) } })
    }
  }

  function receive(notification: AppNotification) {
    notifications.value = [notification, ...notifications.value.filter(item => item.id !== notification.id)]
    unreadCount.value += 1

    const { t } = i18n.global
    toast.info(t('notifications.toastTitle', { name: notification.actorName ?? '' }), {
      duration: 8000,
      description: notification.noteTitle ?? undefined,
      action: {
        label: t('notifications.open'),
        onClick: () => open(notification),
      },
    })
  }

  async function runStream() {
    let attempt = 0

    while (true) {
      if (!isStreaming) {
        return
      }

      streamController = new AbortController()
      try {
        await fetchUnreadCount()
        if (hasLoaded.value) {
          void fetchNotifications()
        }
        await streamNotifications({
          signal: streamController.signal,
          onOpen: () => {
            attempt = 0
            isConnected.value = true
          },
          onNotification: receive,
        })
      }
      catch {
      }
      finally {
        isConnected.value = false
      }

      if (!isStreaming) {
        return
      }

      attempt += 1
      const delay = Math.min(MAX_RECONNECT_DELAY_MS, 1000 * 2 ** Math.min(attempt, 5))
      await new Promise(resolve => setTimeout(resolve, delay))
    }
  }

  function start() {
    if (isStreaming) {
      return
    }
    isStreaming = true
    void runStream()
  }

  function stop() {
    isStreaming = false
    streamController?.abort()
    streamController = null
  }

  function reset() {
    stop()
    listRequestId++
    notifications.value = []
    unreadCount.value = 0
    isLoading.value = false
    hasLoaded.value = false
    error.value = null
  }

  return {
    notifications,
    unreadCount,
    isLoading,
    hasLoaded,
    error,
    isConnected,
    fetchNotifications,
    fetchUnreadCount,
    markRead,
    markAllRead,
    open,
    start,
    stop,
    reset,
  }
})
