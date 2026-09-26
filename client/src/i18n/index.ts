import { createI18n } from 'vue-i18n'
import en from './locales/en'
import km from './locales/km'

export const SUPPORTED_LOCALES = ['en', 'km'] as const

export type AppLocale = (typeof SUPPORTED_LOCALES)[number]

export const LOCALE_LABELS: Record<AppLocale, string> = {
  en: 'English',
  km: 'ខ្មែរ',
}

const INTL_LOCALES: Record<AppLocale, string> = {
  en: 'en-US',
  km: 'km-KH',
}

export const i18n = createI18n({
  legacy: false,
  locale: 'en',
  fallbackLocale: 'en',
  messages: { en, km },
})

export function isSupportedLocale(value: unknown): value is AppLocale {
  return typeof value === 'string' && (SUPPORTED_LOCALES as readonly string[]).includes(value)
}

export function applyLocale(locale: AppLocale) {
  i18n.global.locale.value = locale
  document.documentElement.lang = locale
}

export function currentIntlLocale() {
  const locale = i18n.global.locale.value
  return isSupportedLocale(locale) ? INTL_LOCALES[locale] : INTL_LOCALES.en
}
