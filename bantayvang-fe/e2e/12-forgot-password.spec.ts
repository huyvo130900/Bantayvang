import { test, expect } from '@playwright/test'
import { execSync } from 'child_process'
import { mintToken } from './jwt-helper'
import { apiGet } from './api-helper'

const SQLCMD_CONN = '-S localhost -U PhucHuy -P 123456 -d HeThongBanTayVang'
const TEST_EMAIL = 'nguyenvanan@hospital.com' // email that cua user test 381 (bs.nguyenvanan)
const KNOWN_CODE = '123456'
const NEW_PASSWORD = 'Xr9!vQmzTqa1'

// Email dev khong gui that (EnableEmailSending=false) va OTP chi duoc log ra console BE, khong
// tra ve qua API - khong the doc qua UI/API thuan. Nhu ke hoach: chen thang 1 dong
// EmailVerificationCodes voi ma biet truoc, dung dung hash bcrypt qua endpoint dev co san.
test.describe('Quên mật khẩu (luồng thật qua UI, mã OTP chèn sẵn qua SQL)', () => {
  let originalPasswordHash: string

  test.beforeAll(async () => {
    const out = execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; SELECT Password FROM Users WHERE Id=381;" -h -1 -W`,
      { encoding: 'utf8' }
    )
    originalPasswordHash = out
      .split('\n')
      .map((l) => l.trim())
      .filter((l) => l.length > 0 && !/rows? affected/i.test(l))[0]
  })

  // Chen ma xac nhan biet truoc VAO DB - phai goi SAU khi UI da that su bam "Gui ma xac nhan",
  // vi buoc do goi that toi backend va tu tao 1 dong EmailVerificationCodes MOI voi ma ngau
  // nhien khong biet truoc. VerifyCodeAsync lay dong MOI NHAT theo CreatedAt DESC, nen phai xoa
  // dong that vua duoc tao va thay bang dong cua minh de dong cua minh la dong "moi nhat" duy nhat.
  async function seedKnownCode() {
    const adminToken = mintToken({ userId: 1, username: 'admin', fullName: 'Admin', roleId: 1, roleName: 'Admin' })
    const hashRes = await apiGet(`/Seed/generate-hash/${KNOWN_CODE}`, adminToken)
    const codeHash = hashRes.body?.hash
    if (!codeHash) {
      throw new Error(`seedKnownCode: could not generate OTP hash: ${JSON.stringify(hashRes.body)}`)
    }
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM EmailVerificationCodes WHERE Email=N'${TEST_EMAIL}' AND Purpose='PasswordReset'; INSERT INTO EmailVerificationCodes (Email, CodeHash, Purpose, CreatedAt, ExpiresAt, IsVerified, IsUsed, FailedAttempts) VALUES (N'${TEST_EMAIL}', '${codeHash}', 'PasswordReset', DATEADD(HOUR,7,GETUTCDATE()), DATEADD(HOUR,7,DATEADD(MINUTE,10,GETUTCDATE())), 0, 0, 0);" -h -1 -W`,
      { encoding: 'utf8' }
    )
  }

  test.afterAll(() => {
    // Khoi phuc mat khau that cua user 381 - an toan vi moi spec khac dung token tu ky
    // (mintToken), khong phu thuoc mat khau that cua tai khoan nay.
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; UPDATE Users SET Password='${originalPasswordHash}' WHERE Id=381;" -h -1 -W`,
      { encoding: 'utf8' }
    )
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM EmailVerificationCodes WHERE Email=N'${TEST_EMAIL}' AND Purpose='PasswordReset';" -h -1 -W`,
      { encoding: 'utf8' }
    )
  })

  test('Đi hết luồng quên mật khẩu thật qua UI với mã đã biết trước', async ({ page }) => {
    await page.goto('/quen-mat-khau')

    await page.locator('#email').fill(TEST_EMAIL)
    await page.getByRole('button', { name: 'Gửi mã xác nhận' }).click()
    await expect(page.getByText('Nhập mã xác nhận')).toBeVisible({ timeout: 10_000 })

    await seedKnownCode()
    await page.locator('#code').fill(KNOWN_CODE)
    await page.getByRole('button', { name: 'Xác nhận' }).click()
    await expect(page.getByText('Đặt mật khẩu mới').first()).toBeVisible({ timeout: 10_000 })

    await page.locator('#newPassword').fill(NEW_PASSWORD)
    await page.locator('#confirmPassword').fill(NEW_PASSWORD)
    await page.getByRole('button', { name: 'Đặt lại mật khẩu' }).click()
    await expect(page.getByText('Hoàn tất')).toBeVisible({ timeout: 10_000 })

    // Dang nhap that bang mat khau moi de xac nhan reset thuc su co hieu luc
    await page.getByRole('button', { name: 'Về trang đăng nhập' }).click()
    await expect(page).toHaveURL(/\/login/)
    await page.locator('input[name="username"], input[type="text"]').first().fill('bs.nguyenvanan')
    await page.locator('input[name="password"], input[type="password"]').first().fill(NEW_PASSWORD)
    await page.locator('button[type="submit"]').first().click()
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10_000 })
    await expect(page).toHaveURL(/\/exam-waiting/)
  })
})
