# Workflow 03: Layout & Navigation

## Mục tiêu
Xây dựng layout cho Admin (sidebar + header) và Student (minimal layout). Sidebar navigation theo role.

## Features
- Admin Layout: Sidebar collapsible + Header (user info, logout)
- Student Layout: Minimal (header only, chờ thi)
- Responsive sidebar
- Active menu highlighting
- Breadcrumbs (optional)

## Admin Sidebar Menu
```
Dashboard
├── Tổng quan

Quản lý
├── Người dùng
├── Ngân hàng câu hỏi
├── Đề thi
├── Kỳ thi & Ca thi
├── Phân công thí sinh

Giám sát
├── Kết quả & Chấm điểm
├── Thống kê
├── Audit Log

Hệ thống
├── Thông báo
```

## Student Flow
```
Login → Exam Waiting Room → Start Exam → Exam Interface → Results
```

## Files cần tạo
- `src/components/layout/admin-layout.tsx` - Layout wrapper cho admin pages
- `src/components/layout/student-layout.tsx` - Layout wrapper cho student pages
- `src/components/layout/sidebar.tsx` - Admin sidebar navigation
- `src/components/layout/header.tsx` - Top header (user dropdown, notifications)
- `src/app/router.tsx` - Full router config với nested routes

## Router Structure

```tsx
// src/app/router.tsx
<Routes>
  {/* Public */}
  <Route path="/login" element={<LoginPage />} />

  {/* Admin routes */}
  <Route path="/admin" element={
    <ProtectedRoute allowedRoles={['Admin', 'Teacher', 'Supervisor']}>
      <AdminLayout />
    </ProtectedRoute>
  }>
    <Route path="dashboard" element={<DashboardPage />} />
    <Route path="users" element={<UsersPage />} />
    <Route path="questions" element={<QuestionsPage />} />
    <Route path="exams" element={<ExamsPage />} />
    <Route path="ky-thi" element={<KyThiPage />} />
    <Route path="assignments" element={<AssignmentsPage />} />
    <Route path="grading" element={<GradingPage />} />
    <Route path="statistics" element={<StatisticsPage />} />
    <Route path="audit-log" element={<AuditLogPage />} />
    <Route path="notifications" element={<NotificationsPage />} />
  </Route>

  {/* Student routes */}
  <Route path="/exam-waiting" element={
    <ProtectedRoute allowedRoles={['Student']}>
      <StudentLayout />
    </ProtectedRoute>
  }>
    <Route index element={<ExamWaitingPage />} />
  </Route>

  <Route path="/exam/:baithiId" element={
    <ProtectedRoute allowedRoles={['Student']}>
      <ExamTakingPage />
    </ProtectedRoute>
  } />

  <Route path="/exam-result/:baithiId" element={
    <ProtectedRoute>
      <ExamResultPage />
    </ProtectedRoute>
  } />

  {/* Fallback */}
  <Route path="/unauthorized" element={<UnauthorizedPage />} />
  <Route path="*" element={<Navigate to="/login" />} />
</Routes>
```

## Kết quả
- Admin có sidebar navigation đầy đủ
- Student có giao diện minimal chờ thi
- Routes được bảo vệ theo role
- Layout responsive

## Tiếp theo → Workflow 04: User Management
