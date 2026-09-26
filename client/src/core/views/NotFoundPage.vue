<script setup lang="ts">
import { FileText } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import { Button } from '@/core/components/ui/button'

const { t } = useI18n()
</script>

<template>
  <AppLayout>
    <div class="flex min-h-[calc(100vh-16rem)] flex-col items-center justify-center gap-4 text-center">
      <div class="flex h-14 w-14 items-center justify-center rounded-full bg-muted text-muted-foreground">
        <FileText class="h-7 w-7" />
      </div>
      <div class="space-y-1">
        <h1 class="text-2xl font-bold tracking-tight">
          {{ t('notFound.title') }}
        </h1>
        <p class="text-sm text-muted-foreground">
          {{ t('notFound.description') }}
        </p>
      </div>
      <Button as-child>
        <RouterLink to="/">
          {{ t('notFound.back') }}
        </RouterLink>
      </Button>
    </div>
  </AppLayout>
</template>
