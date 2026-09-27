<script setup lang="ts">
import type { Note, NoteShare, SharePermission } from '../models/note'
import { AlertCircle, Check, Copy, Link2, Link2Off, Loader2, Share2, UserPlus, X } from '@lucide/vue'
import { useClipboard } from '@vueuse/core'
import { storeToRefs } from 'pinia'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { Button } from '@/core/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/core/components/ui/dialog'
import { Input } from '@/core/components/ui/input'
import { Label } from '@/core/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/core/components/ui/select'
import { Skeleton } from '@/core/components/ui/skeleton'
import { describeError, isApiError } from '@/core/services/apiError'
import { useShareStore } from '../store/shareStore'

type ShareTab = 'people' | 'link'

const props = defineProps<{
  note: Note | null
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const router = useRouter()
const shareStore = useShareStore()
const {
  shareToken,
  isLoadingLink,
  isSavingLink,
  linkError,
  shares,
  isLoadingShares,
  isSavingShare,
  sharesError,
} = storeToRefs(shareStore)
const { copy, copied } = useClipboard({ legacy: true })

const tabs: Array<{ value: ShareTab, labelKey: 'notes.share.tabs.people' | 'notes.share.tabs.link' }> = [
  { value: 'people', labelKey: 'notes.share.tabs.people' },
  { value: 'link', labelKey: 'notes.share.tabs.link' },
]
const permissions: Array<{ value: SharePermission, labelKey: 'notes.share.people.canView' | 'notes.share.people.canEdit' }> = [
  { value: 'view', labelKey: 'notes.share.people.canView' },
  { value: 'edit', labelKey: 'notes.share.people.canEdit' },
]

const tab = ref<ShareTab>('people')
const email = ref('')
const newPermission = ref<SharePermission>('view')
const personError = ref<string | null>(null)

const shareUrl = computed(() => {
  if (!shareToken.value) {
    return ''
  }
  const path = router.resolve({ name: 'shared-note', params: { token: shareToken.value } }).href
  return new URL(path, window.location.origin).href
})

function permissionLabel(permission: SharePermission) {
  return permission === 'edit' ? t('notes.share.people.canEdit') : t('notes.share.people.canView')
}

function personName(share: NoteShare) {
  return share.displayName || share.username
}

function initials(share: NoteShare) {
  return personName(share).split(/\s+/).map(part => part[0]).join('').slice(0, 2).toUpperCase()
}

function loadShareLink() {
  if (props.note) {
    shareStore.fetchShareLink(props.note.id)
  }
}

function loadShares() {
  if (props.note) {
    shareStore.fetchShares(props.note.id)
  }
}

watch([open, () => props.note?.id], ([isOpen]) => {
  if (isOpen) {
    tab.value = 'people'
    email.value = ''
    newPermission.value = 'view'
    personError.value = null
    loadShares()
    loadShareLink()
  }
}, { immediate: true })

async function handleAddPerson() {
  if (!props.note || !email.value.trim()) {
    return
  }
  personError.value = null
  try {
    const share = await shareStore.addShare(props.note.id, email.value.trim(), newPermission.value)
    email.value = ''
    toast.success(t('notes.toasts.sharedWith', { name: personName(share) }))
  }
  catch (err) {
    if (isApiError(err, 404)) {
      personError.value = t('notes.share.people.userNotFound')
    }
    else if (isApiError(err, 400)) {
      personError.value = t('notes.share.people.cannotShareWithSelf')
    }
    else {
      personError.value = describeError(err)
    }
  }
}

async function handleChangePermission(share: NoteShare, permission: SharePermission) {
  if (!props.note || share.permission === permission) {
    return
  }
  try {
    await shareStore.updateShare(props.note.id, share.userId, permission)
  }
  catch (err) {
    toast.error(describeError(err))
  }
}

async function handleRemovePerson(share: NoteShare) {
  if (!props.note) {
    return
  }
  try {
    await shareStore.removeShare(props.note.id, share.userId)
    toast.success(t('notes.toasts.accessRemoved', { name: personName(share) }))
  }
  catch (err) {
    toast.error(describeError(err))
  }
}

async function handleCreateLink() {
  if (!props.note) {
    return
  }
  try {
    await shareStore.createShareLink(props.note.id)
  }
  catch (err) {
    toast.error(describeError(err))
  }
}

async function handleStopLink() {
  if (!props.note) {
    return
  }
  try {
    await shareStore.revokeShareLink(props.note.id)
    toast.success(t('notes.toasts.sharingStopped'))
  }
  catch (err) {
    toast.error(describeError(err))
  }
}

function selectAll(event: FocusEvent) {
  (event.target as HTMLInputElement).select()
}
</script>

<template>
  <Dialog v-model:open="open">
    <DialogContent class="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle class="flex items-center gap-2">
          <Share2 class="h-5 w-5" />
          {{ t('notes.share.title') }}
        </DialogTitle>
        <DialogDescription class="break-words">
          {{ note?.title }}
        </DialogDescription>
      </DialogHeader>

      <div class="inline-flex w-fit rounded-md border p-0.5" role="tablist" :aria-label="t('notes.share.title')">
        <button
          v-for="item in tabs"
          :key="item.value"
          type="button"
          role="tab"
          class="rounded px-3 py-1.5 text-sm font-medium transition-colors"
          :class="tab === item.value ? 'bg-secondary text-secondary-foreground shadow-xs' : 'text-muted-foreground hover:text-foreground'"
          :aria-selected="tab === item.value"
          @click="tab = item.value"
        >
          {{ t(item.labelKey) }}
        </button>
      </div>

      <div v-if="tab === 'people'" class="space-y-4" role="tabpanel">
        <form class="space-y-2" @submit.prevent="handleAddPerson">
          <Label for="share-email">{{ t('notes.share.people.addLabel') }}</Label>
          <div class="flex flex-col gap-2 sm:flex-row">
            <Input
              id="share-email"
              v-model="email"
              type="email"
              required
              autocomplete="off"
              :placeholder="t('notes.share.people.emailPlaceholder')"
              class="flex-1"
              :aria-invalid="!!personError"
              @input="personError = null"
            />
            <Select v-model="newPermission">
              <SelectTrigger class="w-full sm:w-32" :aria-label="t('notes.share.people.permission')">
                <SelectValue>{{ permissionLabel(newPermission) }}</SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem v-for="item in permissions" :key="item.value" :value="item.value">
                  {{ t(item.labelKey) }}
                </SelectItem>
              </SelectContent>
            </Select>
            <Button type="submit" :disabled="!email.trim() || isSavingShare">
              <Loader2 v-if="isSavingShare" class="h-4 w-4 animate-spin" />
              <UserPlus v-else class="h-4 w-4" />
              <span>{{ t('notes.share.people.add') }}</span>
            </Button>
          </div>
          <p v-if="personError" class="text-sm text-destructive" role="alert">
            {{ personError }}
          </p>
        </form>

        <div class="space-y-2">
          <p class="text-sm font-medium">
            {{ t('notes.share.people.listTitle') }}
          </p>
          <div v-if="isLoadingShares" class="space-y-2">
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
          </div>
          <div
            v-else-if="sharesError"
            class="flex items-center justify-between gap-2 rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive"
          >
            <span class="flex items-center gap-2">
              <AlertCircle class="h-4 w-4 shrink-0" />
              {{ t('notes.share.people.loadError') }}
            </span>
            <Button variant="outline" size="sm" @click="loadShares">
              {{ t('common.retry') }}
            </Button>
          </div>
          <p v-else-if="!shares.length" class="rounded-md border border-dashed p-4 text-center text-sm text-muted-foreground">
            {{ t('notes.share.people.empty') }}
          </p>
          <ul v-else class="max-h-64 divide-y overflow-y-auto rounded-md border">
            <li v-for="share in shares" :key="share.userId" class="flex items-center gap-3 p-2.5">
              <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                {{ initials(share) }}
              </span>
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-medium">
                  {{ personName(share) }}
                </p>
                <p class="truncate text-xs text-muted-foreground">
                  {{ share.email }}
                </p>
              </div>
              <Select
                :model-value="share.permission"
                @update:model-value="value => handleChangePermission(share, value as SharePermission)"
              >
                <SelectTrigger size="sm" class="w-28" :aria-label="t('notes.share.people.permissionFor', { name: personName(share) })">
                  <SelectValue>{{ permissionLabel(share.permission) }}</SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem v-for="item in permissions" :key="item.value" :value="item.value">
                    {{ t(item.labelKey) }}
                  </SelectItem>
                </SelectContent>
              </Select>
              <Button
                variant="ghost"
                size="icon-sm"
                :aria-label="t('notes.share.people.remove', { name: personName(share) })"
                @click="handleRemovePerson(share)"
              >
                <X class="h-4 w-4" />
              </Button>
            </li>
          </ul>
        </div>
      </div>

      <div v-else class="space-y-4" role="tabpanel">
        <p class="text-sm text-muted-foreground">
          {{ t('notes.share.description', { title: note?.title ?? '' }) }}
        </p>

        <div v-if="isLoadingLink" class="space-y-2">
          <Skeleton class="h-4 w-24" />
          <Skeleton class="h-9 w-full" />
        </div>

        <div
          v-else-if="linkError"
          class="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive"
        >
          <AlertCircle class="mt-0.5 h-4 w-4 shrink-0" />
          <span>{{ t('notes.share.loadError') }}</span>
        </div>

        <div v-else-if="shareToken" class="space-y-2">
          <Label for="share-link">{{ t('notes.share.linkLabel') }}</Label>
          <div class="flex gap-2">
            <Input
              id="share-link"
              :model-value="shareUrl"
              readonly
              class="font-mono text-xs"
              @focus="selectAll"
            />
            <Button variant="outline" class="shrink-0" @click="copy(shareUrl)">
              <Check v-if="copied" class="h-4 w-4" />
              <Copy v-else class="h-4 w-4" />
              <span>{{ copied ? t('notes.share.copied') : t('notes.share.copy') }}</span>
            </Button>
          </div>
          <p class="text-xs text-muted-foreground">
            {{ t('notes.share.sharedHint') }}
          </p>
        </div>

        <p v-else class="text-sm text-muted-foreground">
          {{ t('notes.share.notShared') }}
        </p>

        <DialogFooter class="gap-2">
          <Button v-if="linkError" variant="outline" @click="loadShareLink">
            {{ t('common.retry') }}
          </Button>
          <Button
            v-else-if="shareToken"
            variant="outline"
            class="text-destructive hover:text-destructive"
            :disabled="isSavingLink"
            @click="handleStopLink"
          >
            <Loader2 v-if="isSavingLink" class="h-4 w-4 animate-spin" />
            <Link2Off v-else class="h-4 w-4" />
            <span>{{ t('notes.share.stopSharing') }}</span>
          </Button>
          <Button v-else-if="!isLoadingLink" :disabled="isSavingLink" @click="handleCreateLink">
            <Loader2 v-if="isSavingLink" class="h-4 w-4 animate-spin" />
            <Link2 v-else class="h-4 w-4" />
            <span>{{ t('notes.share.createLink') }}</span>
          </Button>
        </DialogFooter>
      </div>
    </DialogContent>
  </Dialog>
</template>
