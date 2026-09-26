import { test, expect } from '@playwright/test'
import { mintToken } from './jwt-helper'
import { loginWithToken } from './auth-helper'

test.describe('Vai trò Quản lý khoa (Dept Manager)', () => {
  test('Đăng nhập -> dashboard đúng khoa, sidebar chỉ có 7 mục dành riêng', async ({ page }) => {
    const token = mintToken({
      userId: 305,
      username: 'THP07',
      fullName: 'Phạm Lâm Lạc Thư',
      roleId: 5,
      roleName: 'DeptManager',
      department: 'Công xa',
      managedDeptId: 13,
    })
    await loginWithToken(page, token, '/dept-manager/dashboard')
    await expect(page).toHaveURL(/\/dept-manager\/dashboard/, { timeout: 10_000 })
    await expect(page.getByText('Công xa').first()).toBeVisible({ timeout: 10_000 })

    // Sidebar chi co dung 7 muc danh cho Dept Manager - khong co Nguoi dung/Khoa/Thong ke/Audit
    // Log/Loai cau hoi (chi Admin moi thay cac muc nay).
    const allowedItems = ['Tổng quan', 'Ngân hàng câu hỏi', 'Kỳ thi', 'Kết quả thi', 'Chấm điểm', 'Duyệt đăng ký', 'Thông báo']
    for (const label of allowedItems) {
      await expect(page.getByRole('link', { name: label })).toBeVisible()
    }
    const forbiddenItems = ['Người dùng', 'Khoa/Phòng ban', 'Thống kê', 'Audit Log', 'Loại câu hỏi']
    for (const label of forbiddenItems) {
      await expect(page.getByRole('link', { name: label })).toHaveCount(0)
    }
  })

  test('Điều hướng thẳng vào /admin/users -> bị chặn, không render trang admin', async ({ page }) => {
    const token = mintToken({
      userId: 305,
      username: 'THP07',
      fullName: 'Phạm Lâm Lạc Thư',
      roleId: 5,
      roleName: 'DeptManager',
      department: 'Công xa',
      managedDeptId: 13,
    })
    await loginWithToken(page, token, '/admin/users')
    await expect(page).not.toHaveURL(/\/admin\/users/, { timeout: 10_000 })
    // ProtectedRoute dieu huong DeptManager ve trang mac dinh cua ho, khong phai trang admin.
    await expect(page).toHaveURL(/\/dept-manager/)
  })
})
