import { useEffect, useState, useCallback } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { examMonitorApi } from '../../api'

export interface ActiveStudentMonitor {
  examSubmissionId: number
  userId: number
  fullName: string
  userCode: string | null
  examPaperCode: string | null
  warningCount: number
  startTime: string | null
  status: string
}

export function useMonitorSignalR(examCampaignId: number) {
  const [students, setStudents] = useState<ActiveStudentMonitor[]>([])
  const [loading, setLoading] = useState(true)

  const fetchActiveSessions = useCallback(async () => {
    try {
      const response = await examMonitorApi.getActiveSessions(examCampaignId)
      if (response.data.success) {
        setStudents(response.data.data || [])
      }
    } catch (error) {
      console.error('Error fetching active sessions:', error)
    } finally {
      setLoading(false)
    }
  }, [examCampaignId])

  useEffect(() => {
    if (!examCampaignId) return

    fetchActiveSessions()

    const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5293'
    const connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/exam-monitor`, {
        accessTokenFactory: () => localStorage.getItem('accessToken') ?? ''
      })
      .withAutomaticReconnect()
      .build()

    connection.on('CheatingWarning', (data) => {
      setStudents(prev => prev.map(s => {
        if (s.examSubmissionId === data.examSubmissionId) {
          return { ...s, warningCount: data.warningCount }
        }
        return s
      }))
    })

    connection.on('ExamStarted', () => {
      // Re-fetch to get new student in the list
      fetchActiveSessions()
    })

    connection.on('ExamSubmitted', (data) => {
      setStudents(prev => prev.map(s => {
        if (s.examSubmissionId === data.examSubmissionId) {
          return { ...s, status: 'Completed' }
        }
        return s
      }))
    })
    
    connection.onreconnected(async () => {
      await connection.invoke('JoinCampaignMonitoring', examCampaignId)
      fetchActiveSessions()
    })

    connection.start()
      .then(() => connection.invoke('JoinCampaignMonitoring', examCampaignId))
      .catch(console.error)

    return () => {
      connection.stop()
    }
  }, [examCampaignId, fetchActiveSessions])

  return { students, setStudents, loading, fetchActiveSessions }
}
