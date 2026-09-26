import { currentIntlLocale, i18n } from '@/plugins/i18n'

const RELATIVE_UNITS: Array<[Intl.RelativeTimeFormatUnit, number]> = [
  ['year', 365 * 24 * 60 * 60],
  ['month', 30 * 24 * 60 * 60],
  ['week', 7 * 24 * 60 * 60],
  ['day', 24 * 60 * 60],
  ['hour', 60 * 60],
  ['minute', 60],
]

export function formatDate(isoString: string): string {
  if (!isoString) {
    return ''
  }
  return new Intl.DateTimeFormat(currentIntlLocale(), { dateStyle: 'medium' }).format(new Date(isoString))
}

export function formatDateTime(isoString: string): string {
  if (!isoString) {
    return ''
  }
  return new Intl.DateTimeFormat(currentIntlLocale(), { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(isoString))
}

export function formatRelative(isoString: string): string {
  if (!isoString) {
    return ''
  }

  const seconds = Math.round((new Date(isoString).getTime() - Date.now()) / 1000)
  const formatter = new Intl.RelativeTimeFormat(currentIntlLocale(), { numeric: 'auto' })

  for (const [unit, unitSeconds] of RELATIVE_UNITS) {
    if (Math.abs(seconds) >= unitSeconds) {
      return formatter.format(Math.round(seconds / unitSeconds), unit)
    }
  }
  return i18n.global.t('time.justNow')
}

export function startOfDayOffset(daysAgo: number): string {
  const date = new Date()
  date.setHours(0, 0, 0, 0)
  date.setDate(date.getDate() - daysAgo)
  return date.toISOString()
}
