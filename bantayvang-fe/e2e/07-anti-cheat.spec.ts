import { test, expect } from '@playwright/test'
import { execSync } from 'child_process'
import { mintToken } from './jwt-helper'
import { loginWithToken } from './auth-helper'
import { dismissFullscreenGateIfPresent } from './exam-taking-helper'
import { loadState, type E2EState } from './state'

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

test.describe('Chống gian lận khi thi', () => {
  let state: E2EState

  test.beforeAll(() => {
    state = loadState()
  })

  test.afterEach(() => {
    if (!submissionId) return
    // CheatWarnings duoc ghi moi lan gui su kien canh bao (blur/tab-switch/...) - phai xoa truoc
    // ExamSubmissions vi co FK rieng, khong nam trong SubmissionDetails/AuditLogs nhu cac spec khac.
    execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM CheatWarnings WHERE ExamSubmissionId=${submissionId}; DELETE FROM SubmissionDetails WHERE ExamSubmissionId=${submissionId}; DELETE FROM AuditLogs WHERE ExamSubmissionId=${submissionId}; DELETE FROM ExamSubmissions WHERE Id=${submissionId};" -h -1 -W`,
      { encoding: 'utf8' }
    )
    submissionId = 0
  })

  test('Chuyển tab (blur) -> cảnh báo tăng lên, đồng bộ lại đúng sau F5', async ({ page }) => {
    const out = execSync(
      `sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; INSERT INTO ExamSubmissions (ExamPaperId, UserId, StartTime, Status, TotalQuestions, ExamPaperCode, ExamCampaignId, IsIndividualResultPublished, WarningCount) VALUES (${state.examPaperId}, 381, DATEADD(HOUR,7,GETUTCDATE()), 'InProgress', 1, '${state.examPaperCode}', ${state.campaignId}, 0, 0); SELECT SCOPE_IDENTITY();" -h -1 -W`,
      { encoding: 'utf8' }
    )
    submissionId = extractIdentity(out)

    const token = mintToken({
      userId: 381,
      username: 'bs.nguyenvanan',
      fullName: 'Nguyễn Văn An',
      roleId: 3,
      roleName: 'Student',
      department: 'Khoa Nội',
    })
    await loginWithToken(page, token, `/exam/${submissionId}`)
    await dismissFullscreenGateIfPresent(page)
    await page.locator('input[type="radio"], input[type="checkbox"]').first().waitFor({ state: 'visible', timeout: 10_000 })

    // Gia lap chuyen sang cua so/tab khac
    await page.evaluate(() => window.dispatchEvent(new Event('blur')))
    await expect(page.getByText(/Cảnh báo gian lận lần 1\/6/)).toBeVisible({ timeout: 10_000 })
    await expect(page.getByText(/1\/6 lần vi phạm/)).toBeVisible()

    // F5 - so canh bao phai duoc dong bo lai tu server, khong ve 0
    await page.reload()
    await dismissFullscreenGateIfPresent(page)
    await expect(page.getByText(/1\/6 lần vi phạm/)).toBeVisible({ timeout: 10_000 })
  })
})
