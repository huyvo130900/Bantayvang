import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/shared/protected-route'
import { AdminLayout } from '@/components/layout/admin-layout'
import { StudentLayout } from '@/components/layout/student-layout'
import { DeptManagerLayout } from '@/components/layout/dept-manager-layout'
import { LoginPage } from '@/features/auth/pages/login-page'
import { UsersPage } from '@/features/users/pages/users-page'
import { QuestionsPage } from '@/features/questions/pages/questions-page'
import { QuestionTypesPage } from '@/features/questions/pages/question-types-page'
import { ExamsPage } from '@/features/exams/pages/exams-page'
import { ExamTakingPage } from '@/features/exam-taking/pages/exam-taking-page'
import { ExamResultPage } from '@/features/exam-taking/pages/exam-result-page'
import { GradingPage } from '@/features/grading/pages/grading-page'
import { KyThiPage } from '@/features/ky-thi/pages/ky-thi-page'
import { NotificationsPage } from '@/features/notifications/pages/notifications-page'
import { StatisticsPage } from '@/features/statistics/pages/statistics-page'
import { AuditLogPage } from '@/features/audit-log/pages/audit-log-page'
import { DashboardPage } from '@/pages/dashboard-page'
import { DeptManagerDashboard } from '@/pages/dept-manager-dashboard'
import { ExamWaitingPage } from '@/pages/exam-waiting-page'
import { UnauthorizedPage } from '@/pages/unauthorized-page'
import { DepartmentsPage } from '@/features/departments/pages/departments-page'
import { ResultsByKyThiPage } from '@/features/results-by-kythi/pages/results-by-kythi-page'
import { ADMIN_ROLES, ROLES } from '@/lib/constants'
import { useAuthListener } from '@/hooks/use-auth-listener'
import { useRealtimeNotifications } from '@/hooks/use-realtime-notifications'

function AppRoutes() {
  useAuthListener()
  useRealtimeNotifications()

  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      {/* ========== ADMIN routes ========== */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute allowedRoles={ADMIN_ROLES}>
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="departments" element={<DepartmentsPage />} />
        <Route path="questions" element={<QuestionsPage />} />
        <Route path="question-types" element={<QuestionTypesPage />} />
        <Route path="exams" element={<ExamsPage />} />
        <Route path="ky-thi" element={<KyThiPage />} />
        <Route path="grading" element={<GradingPage />} />
        <Route path="results" element={<ResultsByKyThiPage />} />
        <Route path="statistics" element={<StatisticsPage />} />
        <Route path="audit-log" element={<AuditLogPage />} />
        <Route path="notifications" element={<NotificationsPage />} />
      </Route>

      {/* ========== DEPT MANAGER routes ========== */}
      <Route
        path="/dept-manager"
        element={
          <ProtectedRoute allowedRoles={[ROLES.DEPT_MANAGER]}>
            <DeptManagerLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard" element={<DeptManagerDashboard />} />
        <Route path="questions" element={<QuestionsPage />} />
        <Route path="ky-thi" element={<KyThiPage />} />
        <Route path="results" element={<ResultsByKyThiPage />} />
        <Route path="grading" element={<GradingPage />} />
        <Route path="notifications" element={<NotificationsPage />} />
      </Route>

      {/* ========== STUDENT routes ========== */}
      <Route
        path="/"
        element={
          <ProtectedRoute allowedRoles={[ROLES.STUDENT]}>
            <StudentLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/exam-waiting" replace />} />
        <Route path="exam-waiting" element={<ExamWaitingPage />} />
        <Route path="dashboard" element={<DashboardPage />} />
      </Route>

      {/* Exam taking (full screen, no layout) */}
      <Route
        path="/exam/:baithiId"
        element={
          <ProtectedRoute allowedRoles={[ROLES.STUDENT]}>
            <ExamTakingPage />
          </ProtectedRoute>
        }
      />

      {/* Result page */}
      <Route
        path="/exam-result/:baithiId"
        element={
          <ProtectedRoute>
            <ExamResultPage />
          </ProtectedRoute>
        }
      />

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  )
}
