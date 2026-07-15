export interface UserDto {
  id: number
  employeeCode: string | null
  username: string | null
  email: string | null
  phoneNumber: string | null
  fullName: string | null
  jobTitle: string | null
  department: string | null
  roleId: number | null
  roleName: string | null
  deptManagerDeptId?: number | null
  deptManagerDeptName?: string | null
  status: boolean | null
  createdAt: string | null
  lastLoginAt: string | null
  isDeleted: boolean
}

export interface CreateUserDto {
  username: string
  password: string
  email?: string
  phoneNumber?: string
  fullName: string
  employeeCode?: string
  jobTitle?: string
  department?: string
  roleId: number
  deptManagerDeptId?: number
  status: boolean
}

export interface UpdateUserDto {
  email?: string
  phoneNumber?: string
  fullName: string
  employeeCode?: string
  jobTitle?: string
  department?: string
  roleId: number
  deptManagerDeptId?: number
  status: boolean
}

export interface UserFilterDto {
  pageNumber: number
  pageSize: number
  roleId?: number
  status?: boolean
  department?: string
  searchKeyword?: string
  includeDeleted?: boolean
}
