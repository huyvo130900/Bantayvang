import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
export const STATE_FILE = path.join(__dirname, '.e2e-state.json')

export interface E2EState {
  campaignId: number
  campaignCode: string
  examPaperId: number
  examPaperCode: string
  mcQuestionId: number
}

// Chi doc sau khi globalSetup da chay xong (goi trong test.beforeAll, KHONG o module scope -
// Playwright load/collect cac file spec truoc khi chay globalSetup, doc o module scope se
// nhieu luc doc truoc khi file duoc tao).
export function loadState(): E2EState {
  return JSON.parse(fs.readFileSync(STATE_FILE, 'utf8'))
}
