export interface PaginationMeta {
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface PaginationQuery {
  page?: number
  pageSize?: number
}

export interface BaseApiResponse<T> {
  error: boolean
  message?: string
  data: T
  meta?: PaginationMeta
}

export interface PaginatedApiResponse<T> extends BaseApiResponse<T[]> {
  data: T[]
  meta: PaginationMeta
}
