export const APP_NAME = 'Kiểm tra nội bộ'

export const ROLES = {
  ADMIN: 'Admin',
  DEPT_MANAGER: 'DeptManager',
  STUDENT: 'Student',
  THI_SINH_NGOAI: 'ThiSinhNgoai',
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
  THI_SINH_NGOAI: 6,
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

export const SUBMISSION_STATUS = {
  IN_PROGRESS: 'InProgress',
  COMPLETED: 'Completed',
  PAUSED: 'Paused',
} as const

export const CLASSIFICATION = {
  XUAT_SAC: 'Xuất sắc',   // >= 9.0
  GIOI: 'Giỏi',            // >= 8.0
  KHA: 'Khá',              // >= 6.5
  TRUNG_BINH: 'Trung bình', // >= 5.0
  KHONG_DAT: 'Không đạt',  // < 5.0
} as const

export function getClassification(score: number): string {
  if (score >= 9.0) return CLASSIFICATION.XUAT_SAC
  if (score >= 8.0) return CLASSIFICATION.GIOI
  if (score >= 6.5) return CLASSIFICATION.KHA
  if (score >= 5.0) return CLASSIFICATION.TRUNG_BINH
  return CLASSIFICATION.KHONG_DAT
}

export const MAX_CHEATING_WARNINGS = 6
