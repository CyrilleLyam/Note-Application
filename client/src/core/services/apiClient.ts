import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import type { BaseApiResponse } from '@/core/models'
import type { AuthResponse } from '@/modules/auth/models/auth'
import axios from 'axios'
import { getAccessToken, getRefreshToken, setAuthTokens } from '@/core/utils/cookies'
import { CaseConverter } from '@/core/utils/utils'
import { useAuthStore } from '@/modules/auth/store/authStore'
import { i18n } from '@/plugins/i18n'
import router from '@/router'
import { ApiError } from './apiError'

interface CustomAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

let isRefreshing = false
let failedQueue: Array<{
  resolve: (value?: unknown) => void
  reject: (reason?: unknown) => void
}> = []

function processQueue(error: unknown, token: string | null = null) {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error)
    }
    else {
      promise.resolve(token)
    }
  })
  failedQueue = []
}

function handleSessionExpired() {
  useAuthStore().clearSession()
  const current = router.currentRoute.value
  if (current.meta.requiresAuth) {
    router.push({ name: 'login', query: current.fullPath === '/' ? undefined : { redirect: current.fullPath } })
  }
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  if (config.data) {
    config.data = CaseConverter.toSnakeCase(config.data)
  }
  if (config.params) {
    config.params = CaseConverter.toSnakeCase(config.params)
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => {
    if (response.data) {
      response.data = CaseConverter.toCamelCase(response.data)
    }
    return response
  },
  async (error: AxiosError<{ message?: string }>) => {
    const originalRequest = error.config as CustomAxiosRequestConfig | undefined
    const status = error.response?.status
    const message = error.response?.data?.message ?? error.message

    if (
      status === 401
      && originalRequest
      && !originalRequest._retry
      && !originalRequest.url?.includes('/auth/login')
      && !originalRequest.url?.includes('/auth/register')
      && !originalRequest.url?.includes('/auth/refresh')
    ) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then((token) => {
            if (originalRequest.headers && token) {
              originalRequest.headers.Authorization = `Bearer ${token}`
            }
            return apiClient(originalRequest)
          })
          .catch(err => Promise.reject(err))
      }

      originalRequest._retry = true

      const currentRefreshToken = getRefreshToken()
      if (!currentRefreshToken) {
        handleSessionExpired()
        return Promise.reject(new ApiError(i18n.global.t('errors.sessionExpired'), 401))
      }

      isRefreshing = true

      try {
        const response = await axios.post<BaseApiResponse<unknown>>(
          `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
          { refresh_token: currentRefreshToken },
          { headers: { 'Content-Type': 'application/json' } },
        )

        const authData: AuthResponse = CaseConverter.toCamelCase(response.data.data)
        setAuthTokens(authData)

        processQueue(null, authData.accessToken)

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${authData.accessToken}`
        }

        return apiClient(originalRequest)
      }
      catch {
        const sessionError = new ApiError(i18n.global.t('errors.sessionExpired'), 401)
        processQueue(sessionError, null)
        handleSessionExpired()
        return Promise.reject(sessionError)
      }
      finally {
        isRefreshing = false
      }
    }

    return Promise.reject(new ApiError(message, status))
  },
)
