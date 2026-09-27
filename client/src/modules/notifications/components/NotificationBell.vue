<script setup lang="ts">
import type { AppNotification } from '../models/notification'
import { Bell, CheckCheck, Share2 } from '@lucide/vue'
import { storeToRefs } from 'pinia'
import { computed } from 'vue'
import { I18nT, useI18n } from 'vue-i18n'
import { Button } from '@/core/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/core/components/ui/dropdown-menu'
import { Skeleton } from '@/core/components/ui/skeleton'
import { formatRelative } from '@/core/utils/date'
import { useNotificationsStore } from '../store/notificationsStore'

const { t } = useI18n()
const notificationsStore = useNotificationsStore()
const { notifications, unreadCount, isLoading, hasLoaded, error } = storeToRefs(notificationsStore)

const badge = computed(() => unreadCount.value > 9 ? '9+' : String(unreadCount.value))

function handleOpenChange(open: boolean) {
  if (open) {
    void notificationsStore.fetchNotifications()
  }
}

function permissionLabel(notification: AppNotification) {
  return notification.permission === 'edit' ? t('notes.share.people.canEdit') : t('notes.share.people.canView')
}

function handleMarkAllRead(event: Event) {
  event.preventDefault()
  void notificationsStore.markAllRead()
}
</script>

<template>
  <DropdownMenu @update:open="handleOpenChange">
    <DropdownMenuTrigger as-child>
      <Button
        variant="ghost"
        size="icon"
        class="relative rounded-full"
        :aria-label="t('notifications.bellLabel', { count: unreadCount })"
      >
        <Bell class="h-5 w-5" />
        <span
          v-if="unreadCount > 0"
          class="absolute -right-0.5 -top-0.5 flex h-[18px] min-w-[18px] items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-semibold leading-none text-white ring-2 ring-background"
        >
          {{ badge }}
        </span>
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" class="w-80 p-0">
      <DropdownMenuLabel class="flex items-center justify-between px-3 py-2.5 text-sm">
        {{ t('notifications.title') }}
        <span v-if="unreadCount > 0" class="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
          {{ unreadCount }}
        </span>
      </DropdownMenuLabel>
      <DropdownMenuSeparator class="m-0" />

      <div v-if="isLoading && !hasLoaded" class="space-y-2 p-3">
        <Skeleton v-for="n in 3" :key="n" class="h-12 w-full" />
      </div>

      <p v-else-if="error && !notifications.length" class="p-4 text-center text-sm text-destructive">
        {{ t('notifications.loadError') }}
      </p>

      <div v-else-if="!notifications.length" class="flex flex-col items-center gap-2 px-6 py-8 text-center">
        <div class="flex h-10 w-10 items-center justify-center rounded-full bg-muted text-muted-foreground">
          <Bell class="h-5 w-5" />
        </div>
        <p class="text-sm font-medium">
          {{ t('notifications.empty') }}
        </p>
        <p class="text-xs text-muted-foreground">
          {{ t('notifications.emptyDescription') }}
        </p>
      </div>

      <div v-else class="max-h-96 overflow-y-auto p-1">
        <DropdownMenuItem
          v-for="notification in notifications"
          :key="notification.id"
          class="cursor-pointer items-start gap-3 rounded-md p-2.5"
          :class="{ 'bg-primary/5': !notification.readAt }"
          @select="notificationsStore.open(notification)"
        >
          <span class="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
            <Share2 class="h-4 w-4" />
          </span>
          <span class="min-w-0 flex-1 space-y-1">
            <I18nT keypath="notifications.noteShared" tag="span" class="block text-sm leading-snug">
              <template #name>
                <span class="font-medium">{{ notification.actorName }}</span>
              </template>
              <template #title>
                <span class="font-medium break-words">"{{ notification.noteTitle }}"</span>
              </template>
            </I18nT>
            <span class="block text-xs text-muted-foreground">
              {{ permissionLabel(notification) }} · {{ formatRelative(notification.createdAt) }}
            </span>
          </span>
          <span
            v-if="!notification.readAt"
            class="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-primary"
            :aria-label="t('notifications.unread')"
          />
        </DropdownMenuItem>
      </div>

      <template v-if="unreadCount > 0 && notifications.length">
        <DropdownMenuSeparator class="m-0" />
        <DropdownMenuItem class="cursor-pointer justify-center gap-2 rounded-none py-2.5 text-sm" @select="handleMarkAllRead">
          <CheckCheck class="h-4 w-4" />
          {{ t('notifications.markAllRead') }}
        </DropdownMenuItem>
      </template>
    </DropdownMenuContent>
  </DropdownMenu>
</template>
