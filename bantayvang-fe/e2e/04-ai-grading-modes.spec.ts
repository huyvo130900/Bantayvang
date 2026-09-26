import { test, expect } from '@playwright/test'
import { execSync } from 'child_process'
import { loginWithForm } from './auth-helper'

const SQLCMD_CONN = '-S localhost -U PhucHuy -P 123456 -d HeThongBanTayVang'
let submissionId: number

function sql(query: string): string {
  return execSync(`sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; ${query}" -h -1 -W`, {
    encoding: 'utf8',
  })
}

// sqlcmd -h -1 -W ket thuc MOI ket qua SELECT/INSERT bang 1 dong "(N rows affected)" - gia tri
// SCOPE_IDENTITY() nam NGAY TRUOC dong do, khong phai dong cuoi cung. Loc bo dong trailer nay
// truoc khi lay gia tri.
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

test.describe('Chấm tự luận bằng AI - 2 chế độ', () => {
  test.beforeAll(() => {
    // Seed 1 bai nop + 1 cau tu luan that dang cho cham, dung cau hoi that (co san suggestedAnswer)
    // gan vao de thi that "LONGTIME_DE_1_9A5536" (chi doc du lieu cau hoi that, khong sua).
    const out = sql(
      `INSERT INTO ExamSubmissions (ExamPaperId, UserId, StartTime, Status, TotalQuestions, ExamPaperCode, ExamCampaignId, IsIndividualResultPublished, WarningCount) ` +
      `VALUES (21, 381, DATEADD(HOUR,7,GETUTCDATE()), 'Completed', 1, 'LONGTIME_DE_1_9A5536', 5, 0, 0); SELECT SCOPE_IDENTITY();`
    )
    submissionId = extractIdentity(out)

    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; INSERT INTO SubmissionDetails (ExamSubmissionId, QuestionId, EssayAnswer, IsSaved, AnswerTime) VALUES (${submissionId}, 90, N'Xoay tro nguoi benh moi 2 gio de phong loet, giu da kho sach.', 1, GETUTCDATE());" -f 65001`,
      { encoding: 'utf8' }
    )
  })

  test.afterAll(() => {
    if (!submissionId) return
    sql(`DELETE FROM SubmissionDetails WHERE ExamSubmissionId=${submissionId}; DELETE FROM AuditLogs WHERE ExamSubmissionId=${submissionId}; DELETE FROM ExamSubmissions WHERE Id=${submissionId};`)
  })

  test('Chế độ "AI gợi ý, tôi duyệt lại": AI chấm xong KHÔNG tự chốt điểm, chờ người chấm xác nhận', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/grading/bulk')

    // Chon che do "AI goi y, toi duyet lai"
    await page.getByRole('button', { name: 'AI gợi ý, tôi duyệt lại' }).click()
    await page.getByRole('button', { name: /Tải lại/ }).click()

    const card = page.locator('div.bg-white.p-5.rounded-xl', { hasText: 'Xoay tro nguoi benh moi 2 gio' })
    await expect(card).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /AI Chấm/ }).click()
    // Cho AI tra ve goi y (co the mat vai giay goi Gemini that)
    await expect(page.getByText(/AI gợi ý:/)).toBeVisible({ timeout: 30_000 })

    // Diem CHUA duoc chot: dau "Chua cham" van con, khong co nut nao dang duoc highlight la diem hien tai
    await expect(page.getByText('Chưa chấm').first()).toBeVisible()
  })

  test('Bấm điểm gợi ý của AI để duyệt thủ công -> điểm được chốt', async ({ page }) => {
    await loginWithForm(page, 'admin', 'admin123')
    await page.goto('/admin/grading/bulk')
    await page.getByRole('button', { name: /Tải lại/ }).click()

    const card = page.locator('div.bg-white.p-5.rounded-xl', { hasText: 'Xoay tro nguoi benh moi 2 gio' })
    await expect(card).toBeVisible({ timeout: 10_000 })

    // Bam nut diem 0.5 hoac 1.0 de xac nhan diem AI goi y (tuy AI tra ve gi truoc do)
    await card.getByRole('button', { name: '0.5' }).click()
    await expect(card.getByText(/Chưa chấm/)).toHaveCount(0, { timeout: 5_000 })
  })
})
