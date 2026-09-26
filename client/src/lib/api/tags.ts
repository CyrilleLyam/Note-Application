import type { BaseApiResponse, Tag } from '@/types'
import { apiClient } from '@/lib/api/client'

export async function getTags(): Promise<BaseApiResponse<Tag[]>> {
  const response = await apiClient.get<BaseApiResponse<Tag[]>>('/tags')
  return response.data
}
