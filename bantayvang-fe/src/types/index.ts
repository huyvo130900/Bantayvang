export interface ApiResponse<T = unknown> {
  success: boolean
  message: string
  data?: T
  errors?: string[]
  pagination?: PaginationDto
}

// Pagination
export interface PaginationDto {
  pageNumber: number
  pageSize: number
  totalRecords: number
  totalPages: number
}

export interface PagedResult<T> {
  items: T[]
  pagination: PaginationDto
}
