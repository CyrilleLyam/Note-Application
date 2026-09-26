import type { AppLocale } from '@/i18n'
import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import { applyLocale, isSupportedLocale } from '@/i18n'

export const useSettingsStore = defineStore('settings', () => {
  const locale = ref<AppLocale>('en')

  watch(locale, value => applyLocale(value), { flush: 'sync' })

  function setLocale(value: AppLocale) {
    locale.value = value
  }

  return { locale, setLocale }
}, {
  persist: {
    key: 'settings',
    pick: ['locale'],
    afterHydrate: ({ store }) => {
      if (!isSupportedLocale(store.locale)) {
        store.locale = 'en'
      }
    },
  },
})
