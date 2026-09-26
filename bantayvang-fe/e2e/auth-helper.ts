import type { Page } from '@playwright/test'

/**
 * Log a page in without driving the real login form - injects a JWT straight into
 * localStorage (same key the app itself writes on real login) then reloads so
 * AuthInitializer picks it up via fetchCurrentUser(). Used for roles whose real password we
 * don't want to touch (student/external-candidate test accounts).
 */
export async function loginWithToken(page: import('@playwright/test').Page, token: string, landingPath = '/') {
  await page.goto('/')
  await page.evaluate((t) => localStorage.setItem('accessToken', t), token)
  await page.goto(landingPath)
}

export async function loginWithForm(page: Page, username: string, password: string) {
  await page.goto('/login')
  await page.locator('input[name="username"], input[type="text"]').first().fill(username)
  await page.locator('input[name="password"], input[type="password"]').first().fill(password)
  await page.locator('button[type="submit"]').first().click()
  // Doi dieu huong sau dang nhap that xong truoc khi test tiep tuc dieu huong noi khac -
  // khong doi o day se rieu tinh trang goto() ke tiep chay dua voi redirect cua login, co luc
  // ket thuc lai o /login vi accessToken chua kip luu xong.
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10_000 })
}
