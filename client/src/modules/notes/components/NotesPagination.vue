<script setup lang="ts">
import type { PaginationMeta } from '@/core/models'
import { ChevronLeft, ChevronRight } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/core/components/ui/button'

defineProps<{
  meta: PaginationMeta
}>()

const page = defineModel<number>('page', { required: true })

const { t } = useI18n()
</script>

<template>
  <nav class="flex items-center justify-between border-t border-border pt-4" :aria-label="t('notes.pagination.page', { page: meta.page, total: meta.totalPages })">
    <p class="text-sm text-muted-foreground">
      {{ t('notes.pagination.page', { page: meta.page, total: meta.totalPages }) }}
    </p>
    <div class="flex gap-2">
      <Button
        variant="outline"
        size="sm"
        :disabled="!meta.hasPreviousPage"
        @click="page -= 1"
      >
        <ChevronLeft class="h-4 w-4" />
        <span>{{ t('notes.pagination.previous') }}</span>
      </Button>
      <Button
        variant="outline"
        size="sm"
        :disabled="!meta.hasNextPage"
        @click="page += 1"
      >
        <span>{{ t('notes.pagination.next') }}</span>
        <ChevronRight class="h-4 w-4" />
      </Button>
    </div>
  </nav>
</template>
