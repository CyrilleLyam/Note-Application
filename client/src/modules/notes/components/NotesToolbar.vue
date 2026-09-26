<script setup lang="ts">
import type { DateFilter, SortOption } from '../store/notesStore'
import { ArrowUpDown, CalendarRange, RotateCcw, Search, Tags, X } from '@lucide/vue'
import { storeToRefs } from 'pinia'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/core/components/ui/button'
import { Input } from '@/core/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/core/components/ui/select'
import { DATE_FILTERS, SORT_OPTIONS, useNotesStore } from '../store/notesStore'

const ALL_TAGS = '__all__'

const { t } = useI18n()
const notesStore = useNotesStore()
const { search, tag, tags, dateFilter, sort, hasActiveFilters } = storeToRefs(notesStore)

const searchWrapper = ref<HTMLElement | null>(null)

const tagValue = computed({
  get: () => tag.value ?? ALL_TAGS,
  set: (value: string) => {
    tag.value = value === ALL_TAGS ? null : value
  },
})

function focusSearch() {
  searchWrapper.value?.querySelector('input')?.focus()
}

defineExpose({ focusSearch })
</script>

<template>
  <div class="flex flex-col gap-3 lg:flex-row lg:items-center">
    <div ref="searchWrapper" class="relative flex-1">
      <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        v-model="search"
        type="search"
        :placeholder="t('notes.searchPlaceholder')"
        class="pl-9 pr-9 [&::-webkit-search-cancel-button]:hidden"
        :aria-label="t('notes.searchLabel')"
        aria-keyshortcuts="/"
        @keydown.esc="search = ''"
      />
      <button
        v-if="search"
        type="button"
        class="absolute right-2 top-1/2 -translate-y-1/2 rounded-sm p-1 text-muted-foreground transition-colors hover:text-foreground"
        :aria-label="t('notes.clearSearch')"
        @click="search = ''"
      >
        <X class="h-4 w-4" />
      </button>
    </div>

    <div class="grid grid-cols-2 gap-3 sm:flex sm:items-center">
      <Select v-if="tags.length" v-model="tagValue">
        <SelectTrigger class="col-span-2 w-full sm:w-40" :aria-label="t('notes.filterByTag')">
          <Tags class="h-4 w-4" />
          <SelectValue class="flex-1 text-left">
            {{ tag ?? t('notes.allTags') }}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          <SelectItem :value="ALL_TAGS">
            {{ t('notes.allTags') }}
          </SelectItem>
          <SelectItem v-for="item in tags" :key="item.name" :value="item.name">
            {{ item.name }} ({{ item.noteCount }})
          </SelectItem>
        </SelectContent>
      </Select>

      <Select :model-value="dateFilter" @update:model-value="value => dateFilter = value as DateFilter">
        <SelectTrigger class="w-full sm:w-44" :aria-label="t('notes.filterByDate')">
          <CalendarRange class="h-4 w-4" />
          <SelectValue class="flex-1 text-left">
            {{ t(DATE_FILTERS[dateFilter].labelKey) }}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          <SelectItem v-for="(option, key) in DATE_FILTERS" :key="key" :value="key">
            {{ t(option.labelKey) }}
          </SelectItem>
        </SelectContent>
      </Select>

      <Select :model-value="sort" @update:model-value="value => sort = value as SortOption">
        <SelectTrigger class="w-full sm:w-48" :aria-label="t('notes.sortLabel')">
          <ArrowUpDown class="h-4 w-4" />
          <SelectValue class="flex-1 text-left">
            {{ t(SORT_OPTIONS[sort].labelKey) }}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          <SelectItem v-for="(option, key) in SORT_OPTIONS" :key="key" :value="key">
            {{ t(option.labelKey) }}
          </SelectItem>
        </SelectContent>
      </Select>

      <Button
        v-if="hasActiveFilters"
        variant="ghost"
        class="col-span-2 sm:col-span-1"
        @click="notesStore.resetFilters()"
      >
        <RotateCcw class="h-4 w-4" />
        <span>{{ t('common.reset') }}</span>
      </Button>
    </div>
  </div>
</template>
