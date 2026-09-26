import { i18n } from '@/plugins/i18n'

export class ApiError extends Error {
  readonly status?: number

  constructor(message: string, status?: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export function isApiError(error: unknown, status?: number): error is ApiError {
  return error instanceof ApiError && (status === undefined || error.status === status)
}

export function describeError(error: unknown): string {
  const { t } = i18n.global

  if (isApiError(error, 429)) {
    return t('errors.tooManyAttempts')
  }
  if (isApiError(error, 404)) {
    return t('errors.notFound')
  }
  if (isApiError(error, 401)) {
    return t('errors.sessionExpired')
  }
  if (error instanceof ApiError && (error.status === undefined || error.status >= 500)) {
    return t('errors.generic')
  }
  return error instanceof Error ? error.message : t('errors.generic')
}
