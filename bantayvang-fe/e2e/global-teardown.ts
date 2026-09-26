import fs from 'fs'
import { execSync } from 'child_process'
import { STATE_FILE } from './state'

const SQLCMD_CONN = '-S localhost -U PhucHuy -P 123456 -d HeThongBanTayVang'

function sql(query: string) {
  execSync(`sqlcmd ${SQLCMD_CONN} -Q "SET QUOTED_IDENTIFIER ON; ${query}" -h -1 -W`, { encoding: 'utf8' })
}

async function globalTeardown() {
  if (!fs.existsSync(STATE_FILE)) return
  const state = JSON.parse(fs.readFileSync(STATE_FILE, 'utf8'))

  // Xoa thang qua SQL thay vi goi API DELETE: campaign test co StartTime <= now (de spec lam-bai
  // vao thi duoc ngay) nen backend tinh no la "dang dien ra" va tu choi DELETE qua API (dung ly do
  // - day la guard that, khong phai bug) - hop ly khi xoa that nhung can bo qua khi don rac test.
  try {
    if (state.examPaperId) {
      sql(`DELETE FROM ExamPaperQuestions WHERE ExamPaperId=${state.examPaperId}; DELETE FROM ExamPapers WHERE Id=${state.examPaperId};`)
    }
    if (state.campaignId) {
      sql(`DELETE FROM ExamCampaigns WHERE Id=${state.campaignId};`)
    }
  } catch (err) {
    console.warn(`[e2e teardown] cleanup failed - check manually (campaignId=${state.campaignId}, examPaperId=${state.examPaperId}):`, err)
  }

  fs.unlinkSync(STATE_FILE)
}

export default globalTeardown
