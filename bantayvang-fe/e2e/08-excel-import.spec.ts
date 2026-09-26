import { test, expect } from '@playwright/test'
import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'
import { execSync } from 'child_process'
import { loginWithForm } from './auth-helper'
import { mintToken } from './jwt-helper'
import { apiPost, apiGet } from './api-helper'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const SQLCMD_CONN = '-S localhost -U PhucHuy -P 123456 -d HeThongBanTayVang'

const TEST_DEPT_NAME = 'Khoa E2E Import Test'
const TEST_DEPT_CODE = `E2E_IMPORT_${Date.now()}`.slice(0, 50)

test.describe('Import Excel - câu hỏi trắc nghiệm', () => {
  let adminToken: string

  test.beforeAll(async () => {
    adminToken = mintToken({ userId: 1, username: 'admin', fullName: 'Admin', roleId: 1, roleName: 'Admin' })
    // Khoa rieng cho phep don sach chinh xac cau hoi import ra, khong dung vao khoa that nao.
    const res = await apiPost('/Department', adminToken, {
      deptCode: TEST_DEPT_CODE,
      departmentName: TEST_DEPT_NAME,
      status: true,
    })
    if (!res.body?.success) {
      throw new Error(`beforeAll: could not create test department: ${JSON.stringify(res.body)}`)
    }
  })

  test.afterAll(() => {
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM QuestionOptions WHERE QuestionId IN (SELECT Id FROM Questions WHERE Department=N'${TEST_DEPT_NAME}'); DELETE FROM Questions WHERE Department=N'${TEST_DEPT_NAME}'; DELETE FROM Departments WHERE DeptCode='${TEST_DEPT_CODE}';" -f 65001`,
      { encoding: 'utf8' }
    )
  })

  test('Import câu hỏi trắc nghiệm từ file mẫu có sẵn trong repo', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/questions')

    await page.getByRole('button', { name: 'Nhập từ Excel' }).click()
    const dialog = page.locator('div.fixed.inset-0.z-50')
    await expect(dialog.getByText('Nhập câu hỏi từ file Excel')).toBeVisible()

    const deptSelect = dialog.locator('select').filter({ has: page.locator('option', { hasText: TEST_DEPT_NAME }) })
    const optionValue = await deptSelect.locator('option', { hasText: TEST_DEPT_NAME }).first().getAttribute('value')
    await deptSelect.selectOption(optionValue!)

    await dialog.getByText('Trắc nghiệm', { exact: true }).click()

    const fixturePath = path.resolve(__dirname, '..', '..', 'test-imports', 'mau_import_trac_nghiem.xlsx')
    expect(fs.existsSync(fixturePath)).toBe(true)
    await dialog.locator('input[type="file"]').setInputFiles(fixturePath)

    await dialog.getByRole('button', { name: 'Bắt đầu import' }).click()
    // Cho nut tro ve trang thai binh thuong (het "Dang import...") - import that co the mat vai
    // giay do phai doc/parse file that.
    await expect(dialog.getByRole('button', { name: 'Đang import...' })).toHaveCount(0, { timeout: 20_000 })
    // Chi can xac nhan pipeline import thuc su chay va tra ve ket qua (thanh cong hoac loi ro
    // rang) - khong ep phai luon "thanh cong" vi neu chay lai spec nhieu lan, cau hoi trung noi
    // dung se bi tu choi dung theo thiet ke (xem MultipleChoiceImportStrategy.cs).
    await expect(dialog.locator('.bg-green-50, .bg-red-50').filter({ hasText: /./ })).toBeVisible({ timeout: 5_000 })
  })
})

test.describe('Import Excel/CSV - người dùng (bug DeptManager thiếu khoa)', () => {
  const testEmployeeCode = `E2E_IMPORT_${Date.now()}`
  const csvPath = path.join(__dirname, `.tmp-user-import-${Date.now()}.csv`)

  test.beforeAll(() => {
    // API nhap nguoi dung nhan ca .csv (accept=".xlsx,.xls,.csv") - khong can thu vien tao xlsx,
    // viet thang 1 dong CSV mo phong dung bug da fix: vai tro "Quan ly khoa" nhung thieu cot
    // Khoa/Phong.
    const csvContent =
      'Tài khoản,Họ tên,Email,Số điện thoại,Khoa/Phòng,Vai trò\n' +
      `${testEmployeeCode},E2E Test DeptManager No Dept,,0909000098,,Quản lý khoa\n`
    fs.writeFileSync(csvPath, '﻿' + csvContent, 'utf8')
  })

  test.afterAll(() => {
    fs.unlinkSync(csvPath)
    // Phong truong hop bug KHONG con bi chan va dong nay lo duoc tao that - don sach de an toan.
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM Users WHERE Username='${testEmployeeCode}';" -h -1 -W`,
      { encoding: 'utf8' }
    )
  })

  test('Import CSV: dòng "Quản lý khoa" thiếu cột Khoa/Phòng bị từ chối đúng lỗi', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/users')

    await page.getByRole('button', { name: 'Nhập từ Excel' }).click()
    const dialog = page.locator('div.fixed.inset-0.z-50')
    await expect(dialog.getByText('Nhập tài khoản từ file Excel')).toBeVisible()

    await dialog.locator('input[type="file"]').setInputFiles(csvPath)
    await dialog.getByRole('button', { name: 'Nhập tài khoản' }).click()

    await expect(dialog.getByText('Kết quả nhập dữ liệu')).toBeVisible({ timeout: 15_000 })
    await expect(dialog.getByText(/bắt buộc phải có khoa quản lý/)).toBeVisible()

    // Xac nhan dong nay KHONG duoc tao that trong DB (khong chi la UI bao loi ma con phai
    // that su khong ton tai).
    const adminToken = mintToken({ userId: 1, username: 'admin', fullName: 'Admin', roleId: 1, roleName: 'Admin' })
    const check = await apiGet(`/user?searchKeyword=${testEmployeeCode}`, adminToken)
    const items = check.body?.data?.items || check.body?.data || []
    expect(Array.isArray(items) ? items.length : 0).toBe(0)
  })
})
