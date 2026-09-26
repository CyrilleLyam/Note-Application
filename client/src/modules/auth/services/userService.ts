import type { BaseApiResponse, User } from './authService'
import { apiClient } from '@/core/services/apiClient'

export interface UpdateProfileInput {
  username?: string
  displayName?: string
  bio?: string
}

export async function getUserProfile(): Promise<BaseApiResponse<User>> {
  const response = await apiClient.get<BaseApiResponse<User>>('/user/profile')
  return response.data
}

export async function updateUserProfile(input: UpdateProfileInput): Promise<BaseApiResponse<User>> {
  const response = await apiClient.put<BaseApiResponse<User>>('/user/profile', input)
  return response.data
}

export async function uploadUserAvatar(file: File): Promise<BaseApiResponse<User>> {
  const formData = new FormData()
  formData.append('file', file)
  const response = await apiClient.post<BaseApiResponse<User>>('/user/profile/avatar', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  })
  return response.data
}

export async function deleteUserAvatar(): Promise<BaseApiResponse<User>> {
  const response = await apiClient.delete<BaseApiResponse<User>>('/user/profile/avatar')
  return response.data
}
