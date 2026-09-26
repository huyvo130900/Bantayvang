import { test, expect } from '@playwright/test'
import { loginWithForm } from './auth-helper'
import { loadState, type E2EState } from './state'

test.describe('Trang thống kê', () => {
  let state: E2EState

  test.beforeAll(() => {
    state = loadState()
  })

  test('Trang thống kê tổng quan hiện đủ các thẻ chính', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/statistics')

    for (const label of ['Người dùng', 'Câu hỏi', 'Đề thi', 'Bài thi']) {
      await expect(page.getByText(label).first()).toBeVisible({ timeout: 10_000 })
    }
  })

  test('Chọn kỳ thi test trong dropdown -> tải thống kê riêng, không lỗi console', async ({ page }) => {
    const errors: string[] = []
    page.on('pageerror', (e) => errors.push(e.message))

    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/statistics')

    const select = page.locator('select').filter({ has: page.locator('option', { hasText: state.campaignCode }) }).first()
    const optionValue = await select.locator('option', { hasText: state.campaignCode }).first().getAttribute('value')
    await select.selectOption(optionValue!)

    await page.waitForTimeout(1500)
    expect(errors).toEqual([])
  })
})
