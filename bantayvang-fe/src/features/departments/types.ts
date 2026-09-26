export interface DepartmentDto {
  id: number
  deptCode: string
  departmentName: string
  description?: string
  status: boolean
  deptManagerId?: number
  managerName?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateDepartmentDto {
  deptCode: string
  departmentName: string
  description?: string
  status: boolean
}

export interface UpdateDepartmentDto {
  departmentName: string
  description?: string
  status: boolean
}

export interface AssignManagerDto {
  deptManagerId: number
}

export interface ExamVisibilityDto {
  isResultPublished: boolean
}

export interface DepartmentDashboardDto {
  deptId: number
  departmentName: string
  totalQuestions: number
  totalExams: number
  totalCandidates: number
  averageScore: number
  recentCampaigns: ExamCampaignSummaryDto[]
}

export interface ExamCampaignSummaryDto {
  id: number
  campaignName: string
  startTime?: string
  endTime?: string
  status: string
  examCount: number
  candidateCount: number
}
