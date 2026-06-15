export const APP_NAME = 'Kiểm tra nội bộ'

export const ROLES = {
  ADMIN: 'Admin',
  DEPT_MANAGER: 'DeptManager',
  STUDENT: 'Student',
  // Obsolete - giữ lại để tránh break dữ liệu cũ, không hiển thị trên UI
  /** @deprecated Use DEPT_MANAGER instead */
  TEACHER: 'Teacher',
  /** @deprecated Use DEPT_MANAGER instead */
  SUPERVISOR: 'Supervisor',
} as const

export type RoleType = (typeof ROLES)[keyof typeof ROLES]

export const ROLE_IDS = {
  ADMIN: 1,
  TEACHER: 2,       // Obsolete
  STUDENT: 3,
  SUPERVISOR: 4,    // Obsolete
  DEPT_MANAGER: 5,
} as const

// Admin full access
export const ADMIN_ROLES: RoleType[] = [ROLES.ADMIN]

// Roles có thể quản lý đề thi / câu hỏi
export const MANAGEMENT_ROLES: RoleType[] = [ROLES.ADMIN, ROLES.DEPT_MANAGER]

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

export const XEPLOAI = {
  XUAT_SAC: 'Xuất sắc',   // >= 9.0
  GIOI: 'Giỏi',            // >= 8.0
  KHA: 'Khá',              // >= 6.5
  TRUNG_BINH: 'Trung bình', // >= 5.0
  KHONG_DAT: 'Không đạt',  // < 5.0
} as const

export function getXepLoai(diem: number): string {
  if (diem >= 9.0) return XEPLOAI.XUAT_SAC
  if (diem >= 8.0) return XEPLOAI.GIOI
  if (diem >= 6.5) return XEPLOAI.KHA
  if (diem >= 5.0) return XEPLOAI.TRUNG_BINH
  return XEPLOAI.KHONG_DAT
}

export const MAX_CHEATING_WARNINGS = 6
