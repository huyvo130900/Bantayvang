import fs from 'fs'
import { mintToken } from './jwt-helper'
import { apiPost, apiGet, ensureAdminPassword } from './api-helper'
import { STATE_FILE } from './state'

async function globalSetup() {
  const adminToken = mintToken({ userId: 1, username: 'admin', fullName: 'Admin', roleId: 1, roleName: 'Admin' })

  // Bao dam dang nhap that qua form (admin/admin123) hoat dong cho spec dang-nhap-that.
  await ensureAdminPassword(adminToken)

  // Tao 1 ky thi test con han (moi campaign that trong DB dev deu da het han - xem ghi chu
  // trong TOM_TAT_DU_AN_VA_THAY_DOI.md) de cac spec lien quan lam-bai/autosave khong bi
  // AutoSubmitExpiredExamsJob nop bai ho ngay lap tuc.
  // BE tu choi StartTime o qua khu; FE (exam-waiting-page.tsx) lai chi cho bam "Vao thi" khi
  // StartTime <= now. Dat StartTime chi vai giay sau "now" luc seed - cac spec truoc do (dang
  // nhap, dialog tao/sua de) chac chan chay lau hon vai giay nen den luc spec lam-bai chay toi,
  // moc nay da qua.
  const now = new Date()
  const start = new Date(now.getTime() + 5 * 1000)
  const end = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000)
  const campaignCode = `E2E_${Date.now()}`

  const campaignRes = await apiPost('/ExamCampaign', adminToken, {
    campaignCode,
    campaignName: `E2E Test Campaign ${campaignCode}`,
    startTime: start.toISOString(),
    endTime: end.toISOString(),
    totalQuestions: 1,
    minPassQuestions: 1,
    durationMinutes: 30,
  })

  if (!campaignRes.body?.success) {
    throw new Error(`global-setup: could not create test campaign: ${JSON.stringify(campaignRes.body)}`)
  }
  const campaignId = campaignRes.body.data.id

  // Lay 1 cau hoi trac nghiem that bat ky de gan vao de thi test (khong dong den ngan hang
  // cau hoi that - chi doc, khong sua).
  const questionsRes = await apiGet('/Question?PageSize=5', adminToken)
  const mcQuestion = (questionsRes.body?.data?.items || []).find((q: any) => (q.options?.length || 0) >= 2)
  if (!mcQuestion) {
    throw new Error('global-setup: could not find a usable multiple-choice question to seed the test exam paper')
  }

  const examPaperCode = `E2E_PAPER_${Date.now()}`
  const examRes = await apiPost('/exam', adminToken, {
    examPaperCode,
    examPaperName: 'E2E test paper',
    durationMinutes: 30,
    status: 'Active',
    examCampaignId: campaignId,
    questionIds: [mcQuestion.id],
    randomQuestionCount: 1,
  })
  if (!examRes.body?.success) {
    throw new Error(`global-setup: could not create test exam paper: ${JSON.stringify(examRes.body)}`)
  }

  fs.writeFileSync(
    STATE_FILE,
    JSON.stringify(
      {
        campaignId,
        campaignCode,
        examPaperId: examRes.body.data.id,
        examPaperCode,
        mcQuestionId: mcQuestion.id,
      },
      null,
      2
    )
  )
}

export default globalSetup
