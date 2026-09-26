import { test, expect } from '@playwright/test'
import { execSync } from 'child_process'
import { mintToken } from './jwt-helper'
import { loginWithToken, loginWithForm } from './auth-helper'

test.describe('Các lỗi bảo mật đã fix', () => {
  test('Học viên không vào được trang quản trị câu hỏi (chặn lộ đáp án)', async ({ page }) => {
    const token = mintToken({
      userId: 381,
      username: 'bs.nguyenvanan',
      fullName: 'Nguyễn Văn An',
      roleId: 3,
      roleName: 'Student',
      department: 'Khoa Nội',
    })
    await loginWithToken(page, token, '/admin/questions')
    // ProtectedRoute dieu huong ve trang mac dinh cua vai tro (khong phai /unauthorized chung) -
    // voi Student la /exam-waiting. Diem quan trong can xac nhan: KHONG bao gio render trang
    // /admin/questions (noi lo IsCorrect/SuggestedAnswer).
    await expect(page).toHaveURL(/\/exam-waiting/, { timeout: 10_000 })
    await expect(page).not.toHaveURL(/\/admin\/questions/)
  })

  test('Đổi tên danh mục "TL" sang tên không nhận diện được là tự luận -> bị chặn', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/question-types')

    const row = page.locator('tbody tr', { hasText: 'TL' }).first()
    await row.locator('button[title="Sửa"]').click()

    // page.locator('input').first() se dinh nham o "Tim kiem loai cau hoi..." (nam truoc trong
    // DOM) thay vi o nhap ten trong form sua - nham dung placeholder rieng cua o nhap ten.
    const nameInput = page.getByPlaceholder('VD: Trắc nghiệm nhiều lựa chọn')
    await nameInput.fill('Tu luan (mo)')
    await page.getByRole('button', { name: 'Cập nhật', exact: true }).click()

    await expect(page.getByText(/Không thể đổi tên/)).toBeVisible({ timeout: 10_000 })

    // Doi lai ten dung ngay (khong duoc thay doi vi da bi chan, nhung dam bao form khong con
    // giu gia tri sai o local state cho lan test sau)
  })

  test('Đăng ký dự thi công khai: CCCD đã tồn tại và CCCD mới trả về thông điệp giống hệt nhau (chống dò)', async ({ request }) => {
    const existing = await request.post('http://localhost:5293/api/ExamRegistration/public', {
      data: {
        fullName: 'E2E Probe Existing',
        idCardNumber: '082204002864', // CCCD that cua user 391
        phoneNumber: '0909000001',
        email: `e2e-existing-${Date.now()}@example.com`,
        password: 'Xr9!vQmz#Tqa',
      },
    })
    const fresh = await request.post('http://localhost:5293/api/ExamRegistration/public', {
      data: {
        fullName: 'E2E Probe Fresh',
        idCardNumber: `9999${Date.now()}`.slice(0, 12),
        phoneNumber: '0909000002',
        email: `e2e-fresh-${Date.now()}@example.com`,
        password: 'Zq4!wPbnKqa#',
      },
    })
    const existingBody = await existing.json()
    const freshBody = await fresh.json()
    expect(existingBody).toEqual(freshBody)

    // Don sach dong dang ky test vua tao (CCCD fresh bat dau bang "9999")
    execSync(
      `sqlcmd -S localhost -U PhucHuy -P 123456 -d HeThongBanTayVang -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM ExamRegistrations WHERE IdCardNumber LIKE '9999%';" -h -1 -W`,
      { encoding: 'utf8' }
    )
  })
})
