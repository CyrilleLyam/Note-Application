import type { AuthResponse, User } from '../models/auth'
import type { LoginInput, RegisterInput } from '../models/validation'
import type { BaseApiResponse } from '@/core/models'
import { apiClient } from '@/core/services/apiClient'

export type { AuthResponse, BaseApiResponse, User }

export async function loginUser(input: LoginInput): Promise<BaseApiResponse<AuthResponse>> {
  const response = await apiClient.post<BaseApiResponse<AuthResponse>>('/auth/login', {
    email: input.email,
    password: input.password,
  })
  return response.data
}

export async function registerUser(input: RegisterInput): Promise<BaseApiResponse<AuthResponse>> {
  const response = await apiClient.post<BaseApiResponse<AuthResponse>>('/auth/register', {
    username: input.username,
    email: input.email,
    password: input.password,
  })
  return response.data
}

export async function getCurrentUser(): Promise<BaseApiResponse<User>> {
  const response = await apiClient.get<BaseApiResponse<User>>('/auth/me')
  return response.data
}

export async function refreshAccessToken(refreshToken: string): Promise<BaseApiResponse<AuthResponse>> {
  const response = await apiClient.post<BaseApiResponse<AuthResponse>>('/auth/refresh', {
    refreshToken,
  })
  return response.data
}

export async function revokeToken(refreshToken: string): Promise<void> {
  await apiClient.post('/auth/revoke', {
    refreshToken,
  })
}
