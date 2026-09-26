import type { AuthResponse, User } from '../models/auth'
import type { LoginInput, RegisterInput } from '../models/validation'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { clearAuthTokens, getRefreshToken, isAuthenticated as hasAuthTokens, setAuthTokens } from '@/core/utils/cookies'
import { useDraftsStore, useNotesStore } from '@/modules/notes'
import { getCurrentUser, loginUser, registerUser, revokeToken } from '../services/authService'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const isAuthenticated = ref(hasAuthTokens())
  const isLoggingOut = ref(false)

  function setSession(auth: AuthResponse) {
    setAuthTokens(auth)
    user.value = auth.user
    isAuthenticated.value = true
  }

  function clearSession() {
    clearAuthTokens()
    user.value = null
    isAuthenticated.value = false
    useNotesStore().reset()
  }

  async function authenticate(request: () => Promise<{ data: AuthResponse }>) {
    const response = await request()
    useNotesStore().reset()
    setSession(response.data)
    return response.data.user
  }

  function login(input: LoginInput) {
    return authenticate(() => loginUser(input))
  }

  function register(input: RegisterInput) {
    return authenticate(() => registerUser(input))
  }

  async function fetchCurrentUser() {
    if (!isAuthenticated.value) {
      return
    }
    try {
      const response = await getCurrentUser()
      user.value = response.data
    }
    catch {
      user.value = null
    }
  }

  async function logout() {
    isLoggingOut.value = true
    try {
      const refreshToken = getRefreshToken()
      if (refreshToken) {
        await revokeToken(refreshToken).catch(() => undefined)
      }
    }
    finally {
      clearSession()
      useDraftsStore().clearAll()
      isLoggingOut.value = false
    }
  }

  function setUser(updatedUser: User) {
    user.value = updatedUser
  }

  return {
    user,
    isAuthenticated,
    isLoggingOut,
    setSession,
    clearSession,
    setUser,
    login,
    register,
    fetchCurrentUser,
    logout,
  }
})
