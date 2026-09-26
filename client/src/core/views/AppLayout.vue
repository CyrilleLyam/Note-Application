<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import Footer from './Footer.vue'
import Navbar from './Navbar.vue'

const { t } = useI18n()
</script>

<template>
  <div class="min-h-screen flex flex-col bg-background text-foreground antialiased">
    <a
      href="#main-content"
      class="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-[60] focus:rounded-md focus:bg-background focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:shadow-md focus:outline-none focus:ring-3 focus:ring-ring/50"
    >
      {{ t('a11y.skipToContent') }}
    </a>

    <Navbar />

    <main id="main-content" tabindex="-1" class="flex-1 mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8 py-8 focus:outline-none">
      <slot />
    </main>

    <Footer />
  </div>
</template>
