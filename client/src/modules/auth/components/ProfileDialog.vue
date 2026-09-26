<script setup lang="ts">
import { AlertCircle, Camera, Loader2, Trash2, User as UserIcon } from '@lucide/vue'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
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
import { Textarea } from '@/core/components/ui/textarea'
import { describeError } from '@/core/services/apiError'
import { deleteUserAvatar, updateUserProfile, uploadUserAvatar } from '../services/userService'
import { useAuthStore } from '../store/authStore'

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const authStore = useAuthStore()

const fileInputRef = ref<HTMLInputElement | null>(null)
const username = ref('')
const displayName = ref('')
const bio = ref('')
const isSaving = ref(false)
const isUploadingAvatar = ref(false)
const errorMessage = ref<string | null>(null)

watch(open, (isOpen) => {
  if (isOpen && authStore.user) {
    username.value = authStore.user.username || ''
    displayName.value = authStore.user.displayName || ''
    bio.value = authStore.user.bio || ''
    errorMessage.value = null
  }
})

function triggerFileInput() {
  fileInputRef.value?.click()
}

async function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file)
    return

  errorMessage.value = null
  isUploadingAvatar.value = true
  try {
    const response = await uploadUserAvatar(file)
    authStore.setUser(response.data)
    toast.success('Avatar updated successfully')
  }
  catch (err) {
    errorMessage.value = describeError(err)
    toast.error(errorMessage.value)
  }
  finally {
    isUploadingAvatar.value = false
    if (fileInputRef.value) {
      fileInputRef.value.value = ''
    }
  }
}

async function handleDeleteAvatar() {
  errorMessage.value = null
  isUploadingAvatar.value = true
  try {
    const response = await deleteUserAvatar()
    authStore.setUser(response.data)
    toast.success('Avatar removed successfully')
  }
  catch (err) {
    errorMessage.value = describeError(err)
    toast.error(errorMessage.value)
  }
  finally {
    isUploadingAvatar.value = false
  }
}

async function handleSave() {
  errorMessage.value = null
  isSaving.value = true
  try {
    const response = await updateUserProfile({
      username: username.value.trim(),
      displayName: displayName.value.trim(),
      bio: bio.value.trim(),
    })
    authStore.setUser(response.data)
    toast.success('Profile updated successfully')
    open.value = false
  }
  catch (err) {
    errorMessage.value = describeError(err)
    toast.error(errorMessage.value)
  }
  finally {
    isSaving.value = false
  }
}
</script>

<template>
  <Dialog v-model:open="open">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>User Profile</DialogTitle>
        <DialogDescription>
          Manage your account profile and avatar.
        </DialogDescription>
      </DialogHeader>

      <div class="space-y-4 py-2">
        <div
          v-if="errorMessage"
          class="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
        >
          <AlertCircle class="h-4 w-4 shrink-0" />
          <span>{{ errorMessage }}</span>
        </div>

        <!-- Avatar upload section -->
        <div class="flex flex-col items-center gap-3">
          <div class="relative group">
            <div class="h-20 w-20 overflow-hidden rounded-full border-2 border-border bg-muted flex items-center justify-center">
              <img
                v-if="authStore.user?.avatarUrl"
                :src="authStore.user.avatarUrl"
                alt="Avatar"
                class="h-full w-full object-cover"
              >
              <UserIcon v-else class="h-10 w-10 text-muted-foreground" />
            </div>
            <button
              type="button"
              class="absolute inset-0 flex items-center justify-center rounded-full bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity text-white"
              :disabled="isUploadingAvatar"
              @click="triggerFileInput"
            >
              <Loader2 v-if="isUploadingAvatar" class="h-6 w-6 animate-spin" />
              <Camera v-else class="h-6 w-6" />
            </button>
          </div>

          <input
            ref="fileInputRef"
            type="file"
            accept="image/png,image/jpeg,image/webp,image/gif"
            class="hidden"
            @change="handleFileChange"
          >

          <div class="flex gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              :disabled="isUploadingAvatar"
              @click="triggerFileInput"
            >
              <Loader2 v-if="isUploadingAvatar" class="mr-2 h-3.5 w-3.5 animate-spin" />
              <Camera v-else class="mr-2 h-3.5 w-3.5" />
              Change Photo
            </Button>
            <Button
              v-if="authStore.user?.avatarUrl"
              type="button"
              variant="ghost"
              size="sm"
              class="text-destructive hover:text-destructive"
              :disabled="isUploadingAvatar"
              @click="handleDeleteAvatar"
            >
              <Trash2 class="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>

        <!-- Username field -->
        <div class="space-y-1.5">
          <Label for="profile-username">{{ t('auth.username') }}</Label>
          <Input id="profile-username" v-model="username" />
        </div>

        <!-- Display Name field -->
        <div class="space-y-1.5">
          <Label for="profile-display-name">Display Name</Label>
          <Input id="profile-display-name" v-model="displayName" placeholder="e.g. Jane Doe" />
        </div>

        <!-- Email field (readonly) -->
        <div class="space-y-1.5">
          <Label for="profile-email">{{ t('auth.email') }}</Label>
          <Input id="profile-email" :model-value="authStore.user?.email" disabled class="opacity-70" />
        </div>

        <!-- Bio field -->
        <div class="space-y-1.5">
          <Label for="profile-bio">Bio</Label>
          <Textarea id="profile-bio" v-model="bio" placeholder="Tell us a little about yourself" rows="3" />
        </div>
      </div>

      <DialogFooter class="gap-2 sm:gap-0">
        <Button variant="outline" type="button" @click="open = false">
          {{ t('common.cancel') }}
        </Button>
        <Button type="button" :disabled="isSaving" @click="handleSave">
          <Loader2 v-if="isSaving" class="mr-2 h-4 w-4 animate-spin" />
          Save Changes
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
