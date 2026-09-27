<script setup lang="ts">
import { useDark } from '@vueuse/core'
import { watch } from 'vue'
import { Toaster } from '@/core/components/ui/sonner'
import { useAuthStore } from '@/modules/auth'
import { useNotificationsStore } from '@/modules/notifications'

const isDark = useDark()
const auth = useAuthStore()
const notifications = useNotificationsStore()

watch(() => auth.isAuthenticated, (isAuthenticated) => {
  if (isAuthenticated) {
    notifications.start()
  }
  else {
    notifications.reset()
  }
}, { immediate: true })
</script>

<template>
  <RouterView />
  <Toaster
    close-button
    position="top-right"
    :theme="isDark ? 'dark' : 'light'"
    :offset="{ top: 80 }"
    :mobile-offset="{ top: 76 }"
  />
</template>
