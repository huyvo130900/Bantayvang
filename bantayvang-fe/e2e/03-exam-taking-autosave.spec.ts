import { test, expect } from '@playwright/test'
import { mintToken } from './jwt-helper'
import { loginWithToken } from './auth-helper'
import { dismissFullscreenGateIfPresent } from './exam-taking-helper'

test.describe('Làm bài thi & autosave', () => {
  test('Học viên vào thi thật, chọn đáp án -> autosave -> F5 -> đáp án được khôi phục', async ({ page }) => {
    const token = mintToken({
      userId: 381,
      username: 'bs.nguyenvanan',
      fullName: 'Nguyễn Văn An',
      roleId: 3,
      roleName: 'Student',
      department: 'Khoa Nội',
    })
    await loginWithToken(page, token, '/exam-waiting')
    await expect(page.getByRole('heading', { name: 'Phòng chờ thi' })).toBeVisible({ timeout: 10_000 })

    // Bam "Vao thi" tren the ky thi test da seed (dieu huong that qua handleStartExam, khong
    // insert thang vao DB, de test dung dung luong nguoi dung thuc te di qua).
    const startBtn = page.getByRole('button', { name: /Vào thi →|Thi lại →/ }).first()
    await startBtn.click()
    await expect(page).toHaveURL(/\/exam\/\d+/, { timeout: 15_000 })

    await dismissFullscreenGateIfPresent(page)

    // Cho cau hoi hien ra, chon 1 dap an bat ky
    const firstOption = page.locator('input[type="radio"], input[type="checkbox"]').first()
    await firstOption.waitFor({ state: 'visible', timeout: 10_000 })
    await firstOption.check()
    await expect(page.getByText('Đã trả lời')).toBeVisible({ timeout: 5_000 })

    const examUrl = page.url()
    // Gia lap F5: dieu huong lai cung URL (Playwright reload() giu nguyen session storage/local
    // storage, dung nhu mot F5 that trong trinh duyet)
    await page.reload()
    await dismissFullscreenGateIfPresent(page)

    // Fix cua session nay: truoc day khong autosave nen F5 se mat het dap an da chon.
    await expect(page.getByText('Đã trả lời')).toBeVisible({ timeout: 10_000 })
    expect(page.url()).toBe(examUrl)
  })
})
