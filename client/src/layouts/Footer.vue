<script setup lang="ts">
import { NotebookPen } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { Separator } from '@/components/ui/separator'

const { t } = useI18n()
const year = new Date().getFullYear()
</script>

<template>
  <footer class="mt-auto bg-muted/30">
    <Separator />
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-6 flex flex-col sm:flex-row items-center justify-between gap-2">
      <div class="flex items-center gap-2 text-sm text-muted-foreground">
        <NotebookPen class="h-4 w-4 text-primary" />
        <span>{{ t('footer.rights', { year }) }}</span>
      </div>
      <p class="text-xs text-muted-foreground">
        Vue · ASP.NET Core · SQL Server
      </p>
    </div>
  </footer>
</template>
