import { api, state, loginAsAdmin } from './lib/test-helper.mjs'
import { runUserTests } from './tests/02-users.test.mjs'
import { runCategoryTests } from './tests/03-categories.test.mjs'
import { runQuestionTests } from './tests/04-questions.test.mjs'
import { runExamTests } from './tests/05-exams.test.mjs'
import { runExamTakingTests } from './tests/06-exam-taking.test.mjs'
import { runGradingTests } from './tests/07-grading.test.mjs'
import { runKyThiTests } from './tests/08-examCampaign.test.mjs'

async function main() {
  console.log('🚀 Logging in as admin once to bypass auth endpoint rate limits...')
  const loginRes = await loginAsAdmin()
  if (!loginRes.success) {
    console.error('❌ Login failed:', loginRes.message)
    process.exit(1)
  }
  console.log('✅ Logged in successfully!')

  console.log('🚀 Running categories, questions, exams, taking, grading, and kythi tests...')
  await runUserTests()
  await runCategoryTests()
  await runQuestionTests()
  await runExamTests()
  await runExamTakingTests()
  await runGradingTests()
  await runKyThiTests()

  console.log('🎉 Done running tests subset!')
}

main().catch(err => {
  console.error(err)
  process.exit(1)
})
