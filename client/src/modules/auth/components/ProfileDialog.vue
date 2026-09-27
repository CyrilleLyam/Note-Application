<script setup lang="ts">
import { AlertCircle, Camera, Loader2, Trash2, User as UserIcon } from '@lucide/vue'
import { toTypedSchema } from '@vee-validate/zod'
import { useForm } from 'vee-validate'
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
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/core/components/ui/form'
import { Input } from '@/core/components/ui/input'
import { Label } from '@/core/components/ui/label'
import { Textarea } from '@/core/components/ui/textarea'
import { describeError } from '@/core/services/apiError'
import { profileSchema } from '../models/validation'
import { deleteUserAvatar, updateUserProfile, uploadUserAvatar } from '../services/userService'
import { useAuthStore } from '../store/authStore'

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const authStore = useAuthStore()

const fileInputRef = ref<HTMLInputElement | null>(null)
const isUploadingAvatar = ref(false)
const errorMessage = ref<string | null>(null)

const { errors, handleSubmit, isSubmitting, resetForm } = useForm({
  validationSchema: toTypedSchema(profileSchema),
  initialValues: {
    username: '',
    displayName: '',
    bio: '',
  },
})

watch(open, (isOpen) => {
  if (isOpen && authStore.user) {
    resetForm({
      values: {
        username: authStore.user.username ?? '',
        displayName: authStore.user.displayName ?? '',
        bio: authStore.user.bio ?? '',
      },
    })
    errorMessage.value = null
  }
})

function triggerFileInput() {
  fileInputRef.value?.click()
}

async function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) {
    return
  }

  errorMessage.value = null
  isUploadingAvatar.value = true
  try {
    const response = await uploadUserAvatar(file)
    authStore.setUser(response.data)
    toast.success(t('profile.avatarUpdated'))
  }
  catch (err) {
    errorMessage.value = describeError(err)
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
    toast.success(t('profile.avatarRemoved'))
  }
  catch (err) {
    errorMessage.value = describeError(err)
  }
  finally {
    isUploadingAvatar.value = false
  }
}

const onSubmit = handleSubmit(async (values) => {
  errorMessage.value = null
  try {
    const response = await updateUserProfile(values)
    authStore.setUser(response.data)
    toast.success(t('profile.updated'))
    open.value = false
  }
  catch (err) {
    errorMessage.value = describeError(err)
  }
})
</script>

<template>
  <Dialog v-model:open="open">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>{{ t('profile.title') }}</DialogTitle>
        <DialogDescription>{{ t('profile.description') }}</DialogDescription>
      </DialogHeader>

      <form class="space-y-4" novalidate @submit="onSubmit">
        <div
          v-if="errorMessage"
          class="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
          role="alert"
        >
          <AlertCircle class="h-4 w-4 shrink-0" />
          <span>{{ errorMessage }}</span>
        </div>

        <div class="flex flex-col items-center gap-3">
          <div class="group relative">
            <div class="flex h-20 w-20 items-center justify-center overflow-hidden rounded-full border-2 border-border bg-muted">
              <img
                v-if="authStore.user?.avatarUrl"
                :src="authStore.user.avatarUrl"
                :alt="t('profile.avatarAlt')"
                class="h-full w-full object-cover"
              >
              <UserIcon v-else class="h-10 w-10 text-muted-foreground" />
            </div>
            <button
              type="button"
              tabindex="-1"
              aria-hidden="true"
              class="absolute inset-0 flex items-center justify-center rounded-full bg-black/40 text-white opacity-0 transition-opacity group-hover:opacity-100"
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
              <Loader2 v-if="isUploadingAvatar" class="h-3.5 w-3.5 animate-spin" />
              <Camera v-else class="h-3.5 w-3.5" />
              <span>{{ t('profile.changePhoto') }}</span>
            </Button>
            <Button
              v-if="authStore.user?.avatarUrl"
              type="button"
              variant="ghost"
              size="icon-sm"
              class="text-destructive hover:text-destructive"
              :aria-label="t('profile.removePhoto')"
              :disabled="isUploadingAvatar"
              @click="handleDeleteAvatar"
            >
              <Trash2 class="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>

        <FormField v-slot="{ componentField }" name="username" :validate-on-model-update="!!errors.username">
          <FormItem>
            <FormLabel>{{ t('auth.username') }}</FormLabel>
            <FormControl>
              <Input v-bind="componentField" autocomplete="username" />
            </FormControl>
            <FormMessage />
          </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="displayName" :validate-on-model-update="!!errors.displayName">
          <FormItem>
            <FormLabel>{{ t('profile.displayName') }}</FormLabel>
            <FormControl>
              <Input v-bind="componentField" autocomplete="name" :placeholder="t('profile.displayNamePlaceholder')" />
            </FormControl>
            <FormMessage />
          </FormItem>
        </FormField>

        <div class="space-y-2">
          <Label for="profile-email">{{ t('auth.email') }}</Label>
          <Input id="profile-email" :model-value="authStore.user?.email" disabled class="opacity-70" />
        </div>

        <FormField v-slot="{ componentField }" name="bio" :validate-on-model-update="!!errors.bio">
          <FormItem>
            <FormLabel>{{ t('profile.bio') }}</FormLabel>
            <FormControl>
              <Textarea v-bind="componentField" rows="3" :placeholder="t('profile.bioPlaceholder')" />
            </FormControl>
            <FormMessage />
          </FormItem>
        </FormField>

        <DialogFooter class="gap-2">
          <Button variant="outline" type="button" @click="open = false">
            {{ t('common.cancel') }}
          </Button>
          <Button type="submit" :disabled="isSubmitting">
            <Loader2 v-if="isSubmitting" class="h-4 w-4 animate-spin" />
            <span>{{ t('profile.saveChanges') }}</span>
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>
