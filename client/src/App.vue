<script setup lang="ts">
import { useDark } from '@vueuse/core'
import { watch, watchEffect } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { Toaster } from '@/core/components/ui/sonner'
import { useAuthStore } from '@/modules/auth'
import { useNotificationsStore } from '@/modules/notifications'

const APP_TITLE = import.meta.env.VITE_APP_TITLE || 'Notes'

const isDark = useDark()
const route = useRoute()
const { t, te } = useI18n()
const auth = useAuthStore()
const notifications = useNotificationsStore()

watchEffect(() => {
  const titleKey = route.meta.titleKey
  document.title = titleKey && te(titleKey) ? `${t(titleKey)} · ${APP_TITLE}` : APP_TITLE
})

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
