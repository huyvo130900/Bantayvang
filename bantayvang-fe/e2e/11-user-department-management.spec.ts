import { test, expect } from '@playwright/test'
import { execSync } from 'child_process'
import { loginWithForm } from './auth-helper'

const SQLCMD_CONN = '-S localhost -U PhucHuy -P 123456 -d HeThongBanTayVang'

test.describe('Quản lý người dùng', () => {
  const username = `E2E_USER_${Date.now()}`

  test.afterAll(() => {
    // An toan kep: xoa hang neu vi ly do gi test giua chung fail va bo sot buoc don cua chinh no.
    // Neu da xoa vinh vien qua UI thi khong con hang nao de xoa (no-op). Neu test fail som (chua
    // kip xoa qua UI), UserRoles duoc tao ngay luc dang ky nen phai xoa truoc de tranh loi FK.
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DECLARE @id INT = (SELECT Id FROM Users WHERE Username='${username}'); DELETE FROM UserRoles WHERE UserId=@id; DELETE FROM Notifications WHERE UserId=@id; DELETE FROM RefreshTokens WHERE UserId=@id; DELETE FROM UserSessions WHERE UserId=@id; DELETE FROM AuditLogs WHERE UserId=@id; DELETE FROM Users WHERE Id=@id;" -h -1 -W`,
      { encoding: 'utf8' }
    )
  })

  test('Tạo -> sửa -> xóa mềm -> xóa vĩnh viễn 1 tài khoản test', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/users')

    // Tao
    await page.getByRole('button', { name: 'Thêm người dùng' }).click()
    const dialog = page.locator('div.fixed.inset-0')
    await dialog.locator('input[placeholder="NV001"]').first().fill(username)
    await dialog.locator('input[type="password"]').fill('Xr9!vQmzTqa1')
    await dialog.locator('input[placeholder="Nguyễn Văn A"]').fill('E2E Test User')
    await dialog.getByRole('button', { name: /Tạo mới|Tạo/, exact: false }).click()
    await expect(dialog).toHaveCount(0, { timeout: 10_000 })

    // Danh sach co phan trang/sap xep - tim theo ten de chac chan thay du, khong phu thuoc trang nao
    await page.getByPlaceholder('Tìm kiếm theo tên, email...').fill(username)
    await expect(page.getByText(username).first()).toBeVisible({ timeout: 10_000 })

    // Sua
    const row = page.locator('tr', { hasText: username })
    await row.locator('button[title="Sửa thông tin"]').click()
    const editDialog = page.locator('div.fixed.inset-0')
    const nameInput = editDialog.locator('input[placeholder="Nguyễn Văn A"]')
    await nameInput.fill('E2E Test User (Sửa)')
    await editDialog.getByRole('button', { name: /Cập nhật/, exact: false }).click()
    await expect(page.getByText('E2E Test User (Sửa)')).toBeVisible({ timeout: 10_000 })

    // Xoa mem
    page.once('dialog', (d) => d.accept())
    await row.locator('button[title="Xóa tài khoản"]').click()
    await expect(row).toHaveCount(0, { timeout: 10_000 })

    // Vao thung rac, xoa vinh vien de don sach hoan toan
    await page.getByText('Hiển thị tài khoản đã xóa').click()
    const trashRow = page.locator('tr', { hasText: username })
    await expect(trashRow).toBeVisible({ timeout: 10_000 })
    page.once('dialog', (d) => d.accept())
    await trashRow.locator('button[title="Xóa vĩnh viễn"]').click()
    await expect(trashRow).toHaveCount(0, { timeout: 10_000 })
  })
})

test.describe('Quản lý khoa/phòng ban', () => {
  const deptCode = `E2E_DEPT_${Date.now()}`.slice(0, 50)
  const deptName = 'Khoa E2E Test Quản Lý'

  test.afterAll(() => {
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM Departments WHERE DeptCode='${deptCode}';" -f 65001`,
      { encoding: 'utf8' }
    )
  })

  test('Tạo -> sửa -> vô hiệu hóa 1 khoa test (không đụng khoa thật)', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/departments')

    await page.getByRole('button', { name: 'Thêm Khoa' }).click()
    const dialog = page.locator('div.fixed.inset-0')
    await expect(dialog.getByText('Thêm Khoa mới')).toBeVisible()
    await dialog.getByPlaceholder('VD: KHOA_NOI').fill(deptCode)
    await dialog.getByPlaceholder('VD: Khoa Nội').fill(deptName)
    await dialog.getByRole('button', { name: 'Tạo Khoa' }).click()
    await expect(dialog).toHaveCount(0, { timeout: 10_000 })
    await page.getByPlaceholder('Tìm kiếm khoa...').fill(deptName)
    await page.getByRole('button', { name: 'Tìm' }).click()
    await expect(page.getByText(deptName)).toBeVisible({ timeout: 10_000 })

    const row = page.locator('tr', { hasText: deptName })
    await row.locator('button[title="Sửa thông tin khoa"]').click()
    const editDialog = page.locator('div.fixed.inset-0')
    await expect(editDialog.getByText('Cập nhật Khoa')).toBeVisible()
    await editDialog.getByPlaceholder('Mô tả khoa...').fill('Mô tả test E2E')
    await editDialog.getByRole('button', { name: 'Cập nhật' }).click()
    await expect(editDialog).toHaveCount(0, { timeout: 10_000 })

    page.once('dialog', (d) => d.accept())
    await row.locator('button[title="Xóa/Vô hiệu khoa"]').click()
    await expect(page.getByText(/vô hiệu hóa/i)).toBeVisible({ timeout: 10_000 })
  })
})
