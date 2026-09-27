<script setup lang="ts">
import { Toaster } from '@/core/components/ui/sonner'
</script>

<template>
  <RouterView />
  <Toaster rich-colors close-button position="top-right" :offset="{ top: 80 }" :mobile-offset="{ top: 76 }" />
</template>
