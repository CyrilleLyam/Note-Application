<script setup lang="ts">
import type { Note } from '../models/note'
import { ArchiveRestore, Calendar, EllipsisVertical, Eye, LogOut, Pencil, Pin, PinOff, Share2, Trash2, Users } from '@lucide/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Badge } from '@/core/components/ui/badge'
import { Button } from '@/core/components/ui/button'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/core/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/core/components/ui/dropdown-menu'
import { formatDate, formatDateTime, formatRelative } from '@/core/utils/date'
import { markdownToText } from '@/core/utils/markdown'

const props = defineProps<{
  note: Note
}>()

const emit = defineEmits<{
  view: [note: Note]
  edit: [note: Note]
  togglePin: [note: Note]
  share: [note: Note]
  trash: [note: Note]
  restore: [note: Note]
  deleteForever: [note: Note]
  leave: [note: Note]
}>()

const MAX_VISIBLE_TAGS = 3

const { t } = useI18n()

const isTrashed = computed(() => !!props.note.deletedAt)
const isOwner = computed(() => props.note.permission === 'owner')
const preview = computed(() => props.note.content ? markdownToText(props.note.content) : '')
const visibleTags = computed(() => props.note.tags.slice(0, MAX_VISIBLE_TAGS))
const hiddenTagCount = computed(() => props.note.tags.length - visibleTags.value.length)
</script>

<template>
  <Card
    class="group relative gap-4 py-5 transition-all hover:shadow-md hover:border-primary/30 has-[button[data-card-link]:focus-visible]:ring-3 has-[button[data-card-link]:focus-visible]:ring-ring/50"
    :class="{ 'border-primary/40 bg-primary/[0.03]': note.isPinned && !isTrashed, 'opacity-80': isTrashed }"
  >
    <CardHeader class="grid-cols-[1fr_auto] gap-2 px-5">
      <CardTitle class="flex items-start gap-1.5 text-base leading-snug">
        <Pin v-if="note.isPinned && !isTrashed" class="mt-0.5 h-4 w-4 shrink-0 fill-current text-primary" :aria-label="t('notes.pinned')" />
        <button
          type="button"
          data-card-link
          class="line-clamp-2 break-words text-left outline-none after:absolute after:inset-0 after:rounded-xl after:content-['']"
          @click="emit('view', note)"
        >
          {{ note.title }}
        </button>
      </CardTitle>
      <DropdownMenu :modal="false">
        <DropdownMenuTrigger as-child>
          <Button
            variant="ghost"
            size="icon-sm"
            class="relative z-10 -mr-2 -mt-1 sm:opacity-0 sm:group-hover:opacity-100 focus-visible:opacity-100 data-[state=open]:opacity-100"
            :aria-label="t('notes.actions.noteActions')"
          >
            <EllipsisVertical class="h-4 w-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" class="w-48">
          <DropdownMenuItem class="cursor-pointer" @select="emit('view', note)">
            <Eye />
            <span>{{ t('notes.actions.view') }}</span>
          </DropdownMenuItem>
          <template v-if="isTrashed">
            <DropdownMenuItem class="cursor-pointer" @select="emit('restore', note)">
              <ArchiveRestore />
              <span>{{ t('notes.actions.restore') }}</span>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem variant="destructive" class="cursor-pointer" @select="emit('deleteForever', note)">
              <Trash2 />
              <span>{{ t('notes.actions.deleteForever') }}</span>
            </DropdownMenuItem>
          </template>
          <template v-else-if="!isOwner">
            <DropdownMenuItem v-if="note.permission === 'edit'" class="cursor-pointer" @select="emit('edit', note)">
              <Pencil />
              <span>{{ t('notes.actions.edit') }}</span>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem variant="destructive" class="cursor-pointer" @select="emit('leave', note)">
              <LogOut />
              <span>{{ t('notes.actions.leave') }}</span>
            </DropdownMenuItem>
          </template>
          <template v-else>
            <DropdownMenuItem class="cursor-pointer" @select="emit('edit', note)">
              <Pencil />
              <span>{{ t('notes.actions.edit') }}</span>
            </DropdownMenuItem>
            <DropdownMenuItem class="cursor-pointer" @select="emit('togglePin', note)">
              <PinOff v-if="note.isPinned" />
              <Pin v-else />
              <span>{{ note.isPinned ? t('notes.actions.unpin') : t('notes.actions.pin') }}</span>
            </DropdownMenuItem>
            <DropdownMenuItem class="cursor-pointer" @select="emit('share', note)">
              <Share2 />
              <span>{{ t('notes.actions.share') }}</span>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem variant="destructive" class="cursor-pointer" @select="emit('trash', note)">
              <Trash2 />
              <span>{{ t('notes.actions.moveToTrash') }}</span>
            </DropdownMenuItem>
          </template>
        </DropdownMenuContent>
      </DropdownMenu>
    </CardHeader>

    <CardContent class="flex-1 space-y-3 px-5">
      <p v-if="preview" class="line-clamp-4 whitespace-pre-line break-words text-sm text-muted-foreground">
        {{ preview }}
      </p>
      <p v-else class="text-sm italic text-muted-foreground/70">
        {{ t('notes.noContent') }}
      </p>
      <ul v-if="note.tags.length" class="relative z-10 flex flex-wrap gap-1.5">
        <li v-for="tag in visibleTags" :key="tag">
          <Badge variant="outline" class="font-normal">
            {{ tag }}
          </Badge>
        </li>
        <li v-if="hiddenTagCount > 0">
          <Badge variant="outline" class="font-normal text-muted-foreground" :title="note.tags.slice(MAX_VISIBLE_TAGS).join(', ')">
            {{ t('notes.moreTags', { n: hiddenTagCount }) }}
          </Badge>
        </li>
      </ul>
    </CardContent>

    <CardFooter class="flex items-center justify-between gap-2 px-5 text-xs text-muted-foreground">
      <span v-if="isTrashed && note.deletedAt" class="flex items-center gap-1.5" :title="formatDateTime(note.deletedAt)">
        <Trash2 class="h-3.5 w-3.5" />
        {{ t('notes.deletedAt', { date: formatDate(note.deletedAt) }) }}
      </span>
      <span
        v-else-if="!isOwner"
        class="flex min-w-0 items-center gap-1.5"
        :title="t('notes.createdAt', { date: formatDateTime(note.createdAt) })"
      >
        <Users class="h-3.5 w-3.5 shrink-0" />
        <span class="truncate">{{ t('notes.sharedBy', { name: note.ownerName }) }}</span>
      </span>
      <span v-else class="flex items-center gap-1.5" :title="t('notes.createdAt', { date: formatDateTime(note.createdAt) })">
        <Calendar class="h-3.5 w-3.5" />
        {{ formatDate(note.createdAt) }}
      </span>
      <Badge v-if="!isOwner" variant="outline" class="shrink-0">
        {{ note.permission === 'edit' ? t('notes.share.people.canEdit') : t('notes.share.people.canView') }}
      </Badge>
      <Badge v-else-if="note.updatedAt && !isTrashed" variant="secondary" :title="t('notes.updatedAt', { date: formatDateTime(note.updatedAt) })">
        {{ t('notes.edited', { time: formatRelative(note.updatedAt) }) }}
      </Badge>
    </CardFooter>
  </Card>
</template>
