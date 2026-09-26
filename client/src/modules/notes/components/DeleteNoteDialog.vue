<script setup lang="ts">
import type { Note } from '../models/note'
import { Loader2 } from '@lucide/vue'
import { storeToRefs } from 'pinia'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from 'vue-sonner'
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/core/components/ui/alert-dialog'
import { Button } from '@/core/components/ui/button'
import { describeError } from '@/core/services/apiError'
import { useNotesStore } from '../store/notesStore'

const props = defineProps<{
  mode: 'note' | 'trash'
  note?: Note | null
}>()

const emit = defineEmits<{
  deleted: [id: number | null]
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const notesStore = useNotesStore()
const { isDeleting: isPending } = storeToRefs(notesStore)

const isEmptyingTrash = computed(() => props.mode === 'trash')

async function handleConfirm() {
  try {
    if (isEmptyingTrash.value) {
      await notesStore.emptyTrash()
      toast.success(t('notes.toasts.trashEmptied'))
      emit('deleted', null)
    }
    else if (props.note) {
      await notesStore.deleteForever(props.note.id)
      toast.success(t('notes.toasts.deleted'))
      emit('deleted', props.note.id)
    }
    open.value = false
  }
  catch (err) {
    toast.error(describeError(err))
  }
}
</script>

<template>
  <AlertDialog v-model:open="open">
    <AlertDialogContent>
      <AlertDialogHeader>
        <AlertDialogTitle>
          {{ isEmptyingTrash ? t('notes.confirm.emptyTitle') : t('notes.confirm.deleteTitle') }}
        </AlertDialogTitle>
        <AlertDialogDescription class="break-words">
          {{ isEmptyingTrash ? t('notes.confirm.emptyDescription') : t('notes.confirm.deleteDescription', { title: note?.title ?? '' }) }}
        </AlertDialogDescription>
      </AlertDialogHeader>
      <AlertDialogFooter>
        <AlertDialogCancel :disabled="isPending">
          {{ t('common.cancel') }}
        </AlertDialogCancel>
        <Button variant="destructive" :disabled="isPending" @click="handleConfirm">
          <Loader2 v-if="isPending" class="h-4 w-4 animate-spin" />
          <span>{{ isEmptyingTrash ? t('notes.confirm.empty') : t('notes.confirm.delete') }}</span>
        </Button>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>
</template>
