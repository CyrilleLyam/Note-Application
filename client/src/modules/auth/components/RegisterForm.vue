<script setup lang="ts">
import { AlertCircle, Loader2, Lock, Mail, User } from '@lucide/vue'
import { toTypedSchema } from '@vee-validate/zod'
import { useForm } from 'vee-validate'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import { Button } from '@/core/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/core/components/ui/card'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/core/components/ui/form'
import { Input } from '@/core/components/ui/input'
import { describeError, isApiError } from '@/core/services/apiError'
import { useAuthRedirect } from '../composables/useAuthRedirect'
import { registerSchema } from '../models/validation'
import { useAuthStore } from '../store/authStore'

const { t } = useI18n()
const auth = useAuthStore()
const redirectAfterAuth = useAuthRedirect()
const submitError = ref<string | null>(null)

const { errors, handleSubmit, isSubmitting } = useForm({
  validationSchema: toTypedSchema(registerSchema),
  initialValues: {
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
  },
})

const onSubmit = handleSubmit(async (values) => {
  submitError.value = null
  try {
    await auth.register(values)
    await redirectAfterAuth()
  }
  catch (err) {
    submitError.value = isApiError(err, 400) && /already registered/i.test(err.message) ? t('auth.emailTaken') : describeError(err)
  }
})
</script>

<template>
  <Card class="w-full max-w-md mx-auto shadow-lg">
    <CardHeader class="space-y-1">
      <CardTitle class="text-2xl font-bold tracking-tight text-center">
        {{ t('auth.registerTitle') }}
      </CardTitle>
      <CardDescription class="text-center">
        {{ t('auth.registerDescription') }}
      </CardDescription>
    </CardHeader>

    <form novalidate @submit="onSubmit">
      <CardContent class="space-y-4">
        <div
          v-if="submitError"
          class="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
        >
          <AlertCircle class="h-4 w-4 shrink-0" />
          <span>{{ submitError }}</span>
        </div>

        <FormField v-slot="{ componentField }" name="username" :validate-on-model-update="!!errors.username">
          <FormItem>
            <FormLabel>{{ t('auth.username') }}</FormLabel>
            <div class="relative">
              <User class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
              <FormControl>
                <Input
                  v-bind="componentField"
                  type="text"
                  autocomplete="username"
                  placeholder="johndoe"
                  class="pl-9"
                />
              </FormControl>
            </div>
            <FormMessage />
          </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="email" :validate-on-model-update="!!errors.email">
          <FormItem>
            <FormLabel>{{ t('auth.email') }}</FormLabel>
            <div class="relative">
              <Mail class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
              <FormControl>
                <Input
                  v-bind="componentField"
                  type="email"
                  autocomplete="email"
                  :placeholder="t('auth.emailPlaceholder')"
                  class="pl-9"
                />
              </FormControl>
            </div>
            <FormMessage />
          </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="password" :validate-on-model-update="!!errors.password">
          <FormItem>
            <FormLabel>{{ t('auth.password') }}</FormLabel>
            <div class="relative">
              <Lock class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
              <FormControl>
                <Input
                  v-bind="componentField"
                  type="password"
                  autocomplete="new-password"
                  placeholder="••••••••"
                  class="pl-9"
                />
              </FormControl>
            </div>
            <FormMessage />
          </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="confirmPassword" :validate-on-model-update="!!errors.confirmPassword">
          <FormItem>
            <FormLabel>{{ t('auth.confirmPassword') }}</FormLabel>
            <div class="relative">
              <Lock class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
              <FormControl>
                <Input
                  v-bind="componentField"
                  type="password"
                  autocomplete="new-password"
                  placeholder="••••••••"
                  class="pl-9"
                />
              </FormControl>
            </div>
            <FormMessage />
          </FormItem>
        </FormField>
      </CardContent>

      <CardFooter class="flex flex-col space-y-4 pt-6">
        <Button type="submit" class="w-full" :disabled="isSubmitting">
          <Loader2 v-if="isSubmitting" class="mr-2 h-4 w-4 animate-spin" />
          <span>{{ isSubmitting ? t('auth.creatingAccount') : t('auth.createAccount') }}</span>
        </Button>

        <p class="text-sm text-center text-muted-foreground">
          {{ t('auth.haveAccount') }}
          <RouterLink to="/login" class="font-medium text-primary hover:underline">
            {{ t('auth.signInLink') }}
          </RouterLink>
        </p>
      </CardFooter>
    </form>
  </Card>
</template>
