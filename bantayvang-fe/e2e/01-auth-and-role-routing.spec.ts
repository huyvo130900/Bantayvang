import { test, expect } from '@playwright/test'
import { mintToken } from './jwt-helper'
import { loginWithForm, loginWithToken } from './auth-helper'

test.describe('Đăng nhập & phân quyền theo vai trò', () => {
  test('Admin đăng nhập bằng form thật -> vào đúng dashboard quản trị', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await expect(page).toHaveURL(/\/admin\/dashboard/, { timeout: 10_000 })
    await expect(page.getByText(/Quản trị viên hệ thống|Admin/i).first()).toBeVisible()
  })

  test('Học viên KHÔNG còn bị 403 khi vào phòng chờ thi (fix nghiêm trọng nhất)', async ({ page }) => {
    const token = mintToken({
      userId: 381,
      username: 'bs.nguyenvanan',
      fullName: 'Nguyễn Văn An',
      roleId: 3,
      roleName: 'Student',
      department: 'Khoa Nội',
    })
    await loginWithToken(page, token, '/exam-waiting')
    await expect(page).toHaveURL(/\/exam-waiting/)
    // Truoc fix: trang nay luon 403/trang trang vi ExamCampaignController.GetAll bi khoa ManagementOnly.
    await expect(page.getByRole('heading', { name: 'Phòng chờ thi' })).toBeVisible({ timeout: 10_000 })
    await expect(page.getByText(/Nguyễn Văn An/).first()).toBeVisible()
  })

  test('Thí sinh ngoài KHÔNG bị văng vào AdminDashboard (fix dashboard-page.tsx)', async ({ page }) => {
    const token = mintToken({
      userId: 391,
      username: '082204002864',
      fullName: 'Huy PhucHuy',
      roleId: 6,
      roleName: 'ThiSinhNgoai',
      department: 'Khoa Cấp cứu',
    })
    await loginWithToken(page, token, '/')
    // Truoc fix: role ThiSinhNgoai render nham AdminDashboard (khong lo du lieu vi BE van chan,
    // nhung UI sai hoan toan).
    await expect(page).not.toHaveURL(/\/admin/)
    await expect(page.getByRole('heading', { name: 'Phòng chờ thi' })).toBeVisible({ timeout: 10_000 })
  })
})
