export const APP_NAME = 'BanTayVang'

export const ROLES = {
  ADMIN: 'Admin',
  TEACHER: 'Teacher',
  STUDENT: 'Student',
  SUPERVISOR: 'Supervisor',
} as const

export type RoleType = (typeof ROLES)[keyof typeof ROLES]

export const ADMIN_ROLES: RoleType[] = [ROLES.ADMIN, ROLES.TEACHER, ROLES.SUPERVISOR]

export const EXAM_STATUS = {
  DRAFT: 'Draft',
  ACTIVE: 'Active',
  INACTIVE: 'Inactive',
} as const

export const BAITHI_STATUS = {
  IN_PROGRESS: 'InProgress',
  COMPLETED: 'Completed',
  PAUSED: 'Paused',
} as const
