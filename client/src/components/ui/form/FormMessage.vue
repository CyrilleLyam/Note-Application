<script lang="ts" setup>
import type { HTMLAttributes } from 'vue'
import { useI18n } from 'vue-i18n'
import { cn } from '@/lib/utils'
import { useFormField } from './useFormField'

const props = defineProps<{
  class?: HTMLAttributes['class']
}>()

const { error, formMessageId } = useFormField()
const { t, te } = useI18n()
</script>

<template>
  <p
    v-if="error"
    :id="formMessageId"
    data-slot="form-message"
    :class="cn('text-destructive text-sm', props.class)"
  >
    {{ te(error) ? t(error) : error }}
  </p>
</template>
