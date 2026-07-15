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
  examCampaignId: number
  campaignCode: string | null
  campaignName: string | null
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
  department: string | null
  examsTaken: number
  averageScore: number
  highestScore: number
}

export const statisticsApi = {
  getDashboard: () =>
    apiClient.get<ApiResponse<DashboardDto>>('/statistics/dashboard'),

  getKyThiStatistics: (examCampaignId: number) =>
    apiClient.get<ApiResponse<ExamStatisticsDto>>(`/statistics/ExamCampaign/${examCampaignId}`),

  getTopPerformers: (top = 10) =>
    apiClient.get<ApiResponse<TopPerformerDto[]>>(`/statistics/top-performers?top=${top}`),
}
