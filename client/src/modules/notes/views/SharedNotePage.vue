<script setup lang="ts">
import { AlertCircle, Calendar, CalendarClock, Eye, FileX } from '@lucide/vue'
import { storeToRefs } from 'pinia'
import { onBeforeUnmount, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute } from 'vue-router'
import { Badge } from '@/core/components/ui/badge'
import { Button } from '@/core/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader } from '@/core/components/ui/card'
import { Skeleton } from '@/core/components/ui/skeleton'
import { formatDateTime } from '@/core/utils/date'
import { useAuthStore } from '@/modules/auth'
import MarkdownContent from '../components/MarkdownContent.vue'
import { useShareStore } from '../store/shareStore'

const { t } = useI18n()
const route = useRoute()
const auth = useAuthStore()
const shareStore = useShareStore()
const { sharedNote, isLoadingSharedNote, sharedNoteError, isSharedNoteMissing } = storeToRefs(shareStore)

const robotsMeta = document.createElement('meta')
robotsMeta.name = 'robots'
robotsMeta.content = 'noindex'

onMounted(() => document.head.appendChild(robotsMeta))
onBeforeUnmount(() => robotsMeta.remove())

function loadSharedNote() {
  shareStore.fetchSharedNote(String(route.params.token))
}

watch(() => route.params.token, loadSharedNote, { immediate: true })
</script>

<template>
  <AppLayout>
    <div class="mx-auto w-full max-w-3xl">
      <Card v-if="sharedNote" class="gap-4">
        <CardHeader class="gap-3">
          <div class="flex flex-wrap items-start justify-between gap-2">
            <h1 class="break-words text-2xl font-semibold leading-snug">
              {{ sharedNote.title }}
            </h1>
            <Badge variant="secondary" class="shrink-0 gap-1">
              <Eye class="h-3.5 w-3.5" />
              {{ t('sharedNote.readOnly') }}
            </Badge>
          </div>
          <CardDescription class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs">
            <span class="font-medium text-foreground">
              {{ t('sharedNote.sharedBy', { author: sharedNote.author }) }}
            </span>
            <span class="flex items-center gap-1.5">
              <Calendar class="h-3.5 w-3.5" />
              {{ t('notes.createdAt', { date: formatDateTime(sharedNote.createdAt) }) }}
            </span>
            <span v-if="sharedNote.updatedAt" class="flex items-center gap-1.5">
              <CalendarClock class="h-3.5 w-3.5" />
              {{ t('notes.updatedAt', { date: formatDateTime(sharedNote.updatedAt) }) }}
            </span>
          </CardDescription>
        </CardHeader>
        <CardContent>
          <MarkdownContent v-if="sharedNote.content" :source="sharedNote.content" />
          <p v-else class="text-sm italic text-muted-foreground">
            {{ t('notes.detail.noContent') }}
          </p>
        </CardContent>
      </Card>

      <Card v-else-if="isLoadingSharedNote" class="gap-4 p-6" :aria-label="t('sharedNote.loading')" aria-busy="true">
        <Skeleton class="h-8 w-2/3" />
        <Skeleton class="h-4 w-1/3" />
        <Skeleton class="h-40 w-full" />
      </Card>

      <div
        v-else-if="sharedNoteError"
        class="flex min-h-[calc(100vh-16rem)] flex-col items-center justify-center gap-4 text-center"
      >
        <div class="flex h-14 w-14 items-center justify-center rounded-full bg-muted text-muted-foreground">
          <FileX v-if="isSharedNoteMissing" class="h-7 w-7" />
          <AlertCircle v-else class="h-7 w-7" />
        </div>
        <div class="space-y-1">
          <h1 class="text-2xl font-bold tracking-tight">
            {{ isSharedNoteMissing ? t('sharedNote.notFoundTitle') : t('sharedNote.loadError') }}
          </h1>
          <p class="text-sm text-muted-foreground">
            {{ isSharedNoteMissing ? t('sharedNote.notFoundDescription') : sharedNoteError }}
          </p>
        </div>
        <Button v-if="!isSharedNoteMissing" variant="outline" @click="loadSharedNote">
          {{ t('common.retry') }}
        </Button>
      </div>

      <p v-if="!isLoadingSharedNote" class="mt-6 text-center text-sm text-muted-foreground">
        <RouterLink v-if="auth.isAuthenticated" to="/" class="font-medium text-primary underline-offset-4 hover:underline">
          {{ t('sharedNote.backToNotes') }}
        </RouterLink>
        <RouterLink v-else :to="{ name: 'register' }" class="font-medium text-primary underline-offset-4 hover:underline">
          {{ t('sharedNote.createYourOwn') }}
        </RouterLink>
      </p>
    </div>
  </AppLayout>
</template>
