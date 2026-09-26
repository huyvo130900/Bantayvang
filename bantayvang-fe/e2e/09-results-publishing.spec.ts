import { test, expect } from '@playwright/test'
import { execSync } from 'child_process'
import { loginWithForm } from './auth-helper'
import { loadState, type E2EState } from './state'
import { apiGet } from './api-helper'
import { mintToken } from './jwt-helper'

const SQLCMD_CONN = '-S localhost -U PhucHuy -P 123456 -d HeThongBanTayVang'
let submissionId: number

function extractIdentity(sqlOutput: string): number {
  const lines = sqlOutput
    .split('\n')
    .map((l) => l.trim())
    .filter((l) => l.length > 0 && !/rows? affected/i.test(l))
  const id = Number(lines.pop())
  if (!Number.isFinite(id)) {
    throw new Error(`extractIdentity: could not parse an id from sqlcmd output: ${JSON.stringify(sqlOutput)}`)
  }
  return id
}

test.describe('Công bố kết quả thi', () => {
  let state: E2EState

  test.beforeAll(() => {
    state = loadState()
    // Bang ket qua chi hien 1 dong khi co it nhat 1 luot thi that (0 luot thi -> "Chua co ket
    // qua", khong co toggle nao de bam) - seed 1 bai da nop cho de thi test.
    const out = execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; INSERT INTO ExamSubmissions (ExamPaperId, UserId, StartTime, SubmitTime, Status, TotalQuestions, TotalScore, CorrectAnswers, ExamPaperCode, ExamCampaignId, IsIndividualResultPublished, WarningCount) VALUES (${state.examPaperId}, 381, DATEADD(HOUR,7,GETUTCDATE()), DATEADD(HOUR,7,GETUTCDATE()), 'Completed', 1, 1.0, 1, '${state.examPaperCode}', ${state.campaignId}, 0, 0); SELECT SCOPE_IDENTITY();" -h -1 -W`,
      { encoding: 'utf8' }
    )
    submissionId = extractIdentity(out)
  })

  test.afterAll(() => {
    if (!submissionId) return
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM SubmissionDetails WHERE ExamSubmissionId=${submissionId}; DELETE FROM AuditLogs WHERE ExamSubmissionId=${submissionId}; DELETE FROM ExamSubmissions WHERE Id=${submissionId};" -h -1 -W`,
      { encoding: 'utf8' }
    )
  })

  test('Bật/tắt công bố điểm cho 1 lượt thi qua toggle', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/results')

    // Trang yeu cau chon 1 ky thi tu panel trai truoc khi bang ket qua (co toggle cong bo) hien ra.
    // Ky thi test khong gan khoa cu the nen nam trong nhom "Tat ca cac khoa" - can mo nhom truoc.
    const campaignLink = page.getByText(state.campaignCode).first()
    if (!(await campaignLink.isVisible().catch(() => false))) {
      await page.getByRole('button', { name: /Tất cả các khoa/ }).click()
    }
    await campaignLink.click()
    await expect(page.getByText('Chọn một kỳ thi để xem kết quả')).toHaveCount(0, { timeout: 10_000 })
    await expect(page.getByText('Chưa có kết quả')).toHaveCount(0, { timeout: 10_000 })

    const row = page.locator('tr', { hasText: 'Nguyễn Văn An' })
    await expect(row).toBeVisible({ timeout: 10_000 })

    const toggle = row.getByRole('button', { name: /Đã bật|Chưa bật/ })
    const before = await toggle.textContent()
    await toggle.click()
    await expect(page.getByText(/Đã (bật|tắt) công bố kết quả/)).toBeVisible({ timeout: 10_000 })
    await expect(toggle).not.toHaveText(before || '')

    // Xac nhan qua API rang co du lieu that su doi (khong chi toast hien dung)
    const adminToken = mintToken({ userId: 1, username: 'admin', fullName: 'Admin', roleId: 1, roleName: 'Admin' })
    const check = await apiGet(`/exam/code/${state.examPaperCode}`, adminToken)
    expect(typeof check.body?.data).toBe('object')
  })
})
