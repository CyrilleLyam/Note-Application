import type { Tag } from '../models/note'
import type { BaseApiResponse } from '@/core/models'
import { apiClient } from '@/core/services/apiClient'

export async function getTags(): Promise<BaseApiResponse<Tag[]>> {
  const response = await apiClient.get<BaseApiResponse<Tag[]>>('/tags')
  return response.data
}
