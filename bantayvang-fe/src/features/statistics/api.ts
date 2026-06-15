import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'

export interface DashboardDto {
  totalUsers: number
  activeUsers: number
  totalQuestions: number
  totalExams: number
  activeExams: number
  totalSubmissions: number
  inProgressExams: number
  completedExams: number
  averageScore: number
  totalCheatingWarnings: number
  recentActivities: RecentActivityDto[]
}

export interface RecentActivityDto {
  activityType: string
  description: string
  timestamp: string
  username: string | null
}

export interface ExamStatisticsDto {
  examId: number
  maDeThi: string | null
  tenDeThi: string | null
  totalParticipants: number
  completedCount: number
  inProgressCount: number
  averageScore: number
  highestScore: number
  lowestScore: number
  passCount: number
  failCount: number
  passRate: number
  scoreDistribution: ScoreDistributionDto[]
}

export interface ScoreDistributionDto {
  range: string
  count: number
  percentage: number
}

export interface TopPerformerDto {
  userId: number
  username: string | null
  fullName: string | null
  khoaPhong: string | null
  examsTaken: number
  averageScore: number
  highestScore: number
}

export const statisticsApi = {
  getDashboard: () =>
    apiClient.get<ApiResponse<DashboardDto>>('/statistics/dashboard'),

  getExamStatistics: (examId: number) =>
    apiClient.get<ApiResponse<ExamStatisticsDto>>(`/statistics/exam/${examId}`),

  getTopPerformers: (top = 10) =>
    apiClient.get<ApiResponse<TopPerformerDto[]>>(`/statistics/top-performers?top=${top}`),
}
