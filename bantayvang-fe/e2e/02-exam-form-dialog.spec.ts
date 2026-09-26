import { test, expect } from '@playwright/test'
import { loginWithForm } from './auth-helper'
import { loadState, type E2EState } from './state'

test.describe('Dialog Tạo/Sửa đề thi (vừa khôi phục sau sự cố hỏng file)', () => {
  let state: E2EState

  test.beforeAll(() => {
    state = loadState()
  })

  test.beforeEach(async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await expect(page).toHaveURL(/\/admin\/dashboard/, { timeout: 10_000 })
    await page.goto('/admin/exams')
  })

  test('Tạo đề thi mới: chọn kỳ thi, chọn câu hỏi từ ngân hàng, submit thành công', async ({ page }) => {
    await page.getByRole('button', { name: 'Tạo đề thi' }).click()
    // Dialog la 1 div.fixed.inset-0 duoc render ngay trong cay JSX cua trang (khong dung portal),
    // nen "select" toan trang co the trung voi select loc trang thai/khoa o phia tren trang exams
    // - can gioi han pham vi tim kiem vao rieng dialog, khong dung page.locator('select') truc tiep.
    const dialog = page.locator('div.fixed.inset-0.z-50')
    await expect(dialog.getByText('Tạo đề thi mới')).toBeVisible()

    // Chon ky thi test da seed san (khong dung campaign that trong DB, tranh dung vao du lieu san xuat)
    const campaignSelect = dialog.locator('select').first()
    const optionValue = await campaignSelect
      .locator('option', { hasText: state.campaignCode })
      .first()
      .getAttribute('value')
    await campaignSelect.selectOption(optionValue!)

    // Doi danh sach cau hoi trong ngan hang tai xong, tick cau dau tien
    await expect(dialog.locator('text=Đang tải...')).toHaveCount(0, { timeout: 10_000 })
    const firstCheckbox = dialog.locator('input[type="checkbox"]').first()
    await firstCheckbox.waitFor({ state: 'visible', timeout: 10_000 })
    await firstCheckbox.check()
    await expect(dialog.getByText(/Đã chọn:\s*1\s*câu/)).toBeVisible()

    await dialog.getByRole('button', { name: 'Tạo đề thi', exact: true }).click()
    await expect(page.getByText(/Tạo đề thi thành công/)).toBeVisible({ timeout: 10_000 })
  })

  test('Sửa đề thi test đã seed: dialog mở đúng chế độ Cập nhật, giữ nguyên câu hỏi đã chọn', async ({ page }) => {
    const row = page.locator('tr', { hasText: state.examPaperCode })
    await expect(row).toBeVisible({ timeout: 10_000 })
    await row.locator('button[title="Chỉnh sửa"]').click()

    await expect(page.getByRole('heading', { name: 'Cập nhật đề thi' })).toBeVisible()
    // Fix vua khoi phuc: che do sua phai tu dong tick san (các) cau hoi da co trong de thi,
    // KHONG duoc de trong "Da chon: 0 cau".
    await expect(page.getByText(/Đã chọn:\s*1\s*câu/)).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: 'Hủy' }).click()
    await expect(page.getByText('Cập nhật đề thi')).toHaveCount(0)
  })
})
