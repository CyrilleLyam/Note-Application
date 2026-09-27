<script setup lang="ts">
import { useDark } from '@vueuse/core'
import { Toaster } from '@/core/components/ui/sonner'

const isDark = useDark()
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
