import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/shared/protected-route'
import { AdminLayout } from '@/components/layout/admin-layout'
import { StudentLayout } from '@/components/layout/student-layout'
import { DeptManagerLayout } from '@/components/layout/dept-manager-layout'
import { LoginPage } from '@/features/auth/pages/login-page'
import { ForgotPasswordPage } from '@/features/auth/pages/forgot-password-page'
import { ChangePasswordPage } from '@/features/auth/pages/change-password-page'
import { UsersPage } from '@/features/users/pages/users-page'
import { QuestionsPage } from '@/features/questions/pages/questions-page'
import { QuestionTypesPage } from '@/features/questions/pages/question-types-page'
import { ExamsPage } from '@/features/exams/pages/exams-page'
import { ExamTakingPage } from '@/features/exam-taking/pages/exam-taking-page'
import { ExamResultPage } from '@/features/exam-taking/pages/exam-result-page'
import { GradingPage } from '@/features/grading/pages/grading-page'
import { KyThiPage } from '@/features/ky-thi/pages/ky-thi-page'
import { ExamMonitorPage } from '@/features/ky-thi/pages/monitor/exam-monitor-page'
import { NotificationsPage } from '@/features/notifications/pages/notifications-page'
import { StatisticsPage } from '@/features/statistics/pages/statistics-page'
import { AuditLogPage } from '@/features/audit-log/pages/audit-log-page'
import { DashboardPage } from '@/pages/dashboard-page'
import { DeptManagerDashboard } from '@/pages/dept-manager-dashboard'
import { ExamWaitingPage } from '@/pages/exam-waiting-page'
import { UnauthorizedPage } from '@/pages/unauthorized-page'
import { DepartmentsPage } from '@/features/departments/pages/departments-page'
import { ResultsByKyThiPage } from '@/features/results-by-kythi/pages/results-by-kythi-page'
import { PublicRegistrationPage } from '@/features/registration/pages/public-registration-page'
import { AdminRegistrationPage } from '@/features/registration/pages/admin-registration-page'
import { ADMIN_ROLES, ROLES } from '@/lib/constants'
import { useAuthListener } from '@/hooks/use-auth-listener'
import { useRealtimeNotifications } from '@/hooks/use-realtime-notifications'
import { BulkEssayGradingPage } from '@/features/grading/pages/bulk-essay-grading-page'

function AppRoutes() {
  useAuthListener()
  useRealtimeNotifications()

  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/dang-ky" element={<PublicRegistrationPage />} />
      <Route path="/quen-mat-khau" element={<ForgotPasswordPage />} />
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
        <Route path="ky-thi/:id/monitor" element={<ExamMonitorPage />} />
        <Route path="grading" element={<GradingPage />} />
        <Route path="grading/bulk" element={<BulkEssayGradingPage />} />
        <Route path="results" element={<ResultsByKyThiPage />} />
        <Route path="statistics" element={<StatisticsPage />} />
        <Route path="dang-ky-thi" element={<AdminRegistrationPage />} />
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
        <Route path="ky-thi/:id/monitor" element={<ExamMonitorPage />} />
        <Route path="results" element={<ResultsByKyThiPage />} />
        <Route path="dang-ky-thi" element={<AdminRegistrationPage />} />
        <Route path="grading" element={<GradingPage />} />
        <Route path="grading/bulk" element={<BulkEssayGradingPage />} />
        <Route path="notifications" element={<NotificationsPage />} />
      </Route>

      {/* ========== STUDENT routes ========== */}
      <Route
        path="/"
        element={
          <ProtectedRoute allowedRoles={[ROLES.STUDENT, ROLES.THI_SINH_NGOAI]}>
            <StudentLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/exam-waiting" replace />} />
        <Route path="exam-waiting" element={<ExamWaitingPage />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="doi-mat-khau" element={<ChangePasswordPage />} />
      </Route>

      {/* Exam taking (full screen, no layout) */}
      <Route
        path="/exam/:examSubmissionId"
        element={
          <ProtectedRoute allowedRoles={[ROLES.STUDENT, ROLES.THI_SINH_NGOAI]}>
            <ExamTakingPage />
          </ProtectedRoute>
        }
      />

      {/* Result page */}
      <Route
        path="/exam-result/:examSubmissionId"
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
