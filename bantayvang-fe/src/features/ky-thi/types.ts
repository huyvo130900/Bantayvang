export interface ExamCampaignDto {
  id: number
  campaignCode: string | null
  campaignName: string | null
  description: string | null
  departmentIds: number[]
  departmentNames: string[]
  accessMode: 'Department' | 'AssignedList'
  isPracticeMode: boolean
  status: string | null
  startTime: string | null
  endTime: string | null
  organizedBy: string | null
  createdAt: string
  totalExamPapers: number
  totalCandidates: number
  examPaperCodes: string[]
  examPaperCode: string | null
  minPassQuestions?: number | null
  totalQuestions?: number | null
  durationMinutes?: number | null
}

export interface ExamGenerationConfig {
  numberOfExams: number
  totalQuestions: number
  multipleChoiceQuestions: number
  essayQuestions: number
  easyQuestions: number
  mediumQuestions: number
  hardQuestions: number
  department?: string
  // Kỳ thi giờ có thể gán 1-n khoa - dùng field này để lọc ngân hàng câu hỏi theo TẤT CẢ khoa
  // của kỳ thi thay vì chỉ 1 chuỗi đơn (department, giữ lại cho luồng Quản lý khoa 1 khoa).
  departmentNames?: string[]
}

export interface ExamCheckResult {
  canGenerate: boolean
  warnings: string[]
}

export interface CreateKyThiDto {
  campaignCode: string
  campaignName: string
  description?: string
  departmentIds?: number[] | null
  accessMode?: 'Department' | 'AssignedList'
  isPracticeMode?: boolean
  startTime?: string | null
  endTime?: string | null
  organizedBy?: string
  minPassQuestions?: number | null
  totalQuestions?: number | null
  durationMinutes?: number | null
}

export interface UpdateKyThiDto extends CreateKyThiDto {
  status: string
}

export interface AssignFromExcelResultDto {
  totalRows: number
  matchedUserCount: number
  notFoundCodes: string[]
}

export interface ExamCampaignEligibilityDto {
  userId: number
  fullName: string | null
  employeeCode: string | null
  department: string | null
  hasSubmitted: boolean
  submissionStatus: string | null
  totalScore: number | null
  submitTime: string | null
}
