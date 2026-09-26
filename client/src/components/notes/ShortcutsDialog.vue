<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()

const shortcuts = [
  { keys: ['N'], labelKey: 'shortcuts.newNote' },
  { keys: ['/'], labelKey: 'shortcuts.search' },
  { keys: ['?'], labelKey: 'shortcuts.help' },
  { keys: ['Ctrl', 'Enter'], labelKey: 'shortcuts.save' },
  { keys: ['Esc'], labelKey: 'shortcuts.close' },
] as const
</script>

<template>
  <Dialog v-model:open="open">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>{{ t('shortcuts.title') }}</DialogTitle>
        <DialogDescription>{{ t('shortcuts.description') }}</DialogDescription>
      </DialogHeader>
      <dl class="divide-y divide-border">
        <div v-for="shortcut in shortcuts" :key="shortcut.labelKey" class="flex items-center justify-between gap-4 py-2.5 text-sm">
          <dt>{{ t(shortcut.labelKey) }}</dt>
          <dd class="flex gap-1">
            <kbd
              v-for="key in shortcut.keys"
              :key="key"
              class="min-w-6 rounded border border-border bg-muted px-1.5 py-0.5 text-center font-mono text-xs"
            >{{ key }}</kbd>
          </dd>
        </div>
      </dl>
    </DialogContent>
  </Dialog>
</template>
