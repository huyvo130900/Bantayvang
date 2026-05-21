/**
 * BanTayVang Full Test Suite Runner
 * Runs all test suites in order
 *
 * Usage: npm test (or: node runner.mjs)
 */
import { runAuthTests } from './tests/01-auth.test.mjs'
import { runUserTests } from './tests/02-users.test.mjs'
import { runCategoryTests } from './tests/03-categories.test.mjs'
import { runQuestionTests } from './tests/04-questions.test.mjs'
import { runExamTests } from './tests/05-exams.test.mjs'
import { runExamTakingTests } from './tests/06-exam-taking.test.mjs'
import { runGradingTests } from './tests/07-grading.test.mjs'
import { runKyThiTests } from './tests/08-kythi.test.mjs'
import { runNotificationTests } from './tests/09-notifications.test.mjs'
import { runStatisticsTests } from './tests/10-statistics.test.mjs'
import { runAuditLogTests } from './tests/11-audit-log.test.mjs'
import { runSecurityTests } from './tests/12-security.test.mjs'
import { runEdgeCaseTests } from './tests/13-edge-cases.test.mjs'
import { runDataIntegrityTests } from './tests/14-data-integrity.test.mjs'
import { runPerformanceTests } from './tests/15-performance.test.mjs'
import { runExamWorkflowTests } from './tests/16-exam-workflow.test.mjs'
import { runAdvancedSecurityTests } from './tests/17-advanced-security.test.mjs'
import { runAdvancedExamTests } from './tests/18-exam-advanced.test.mjs'
import { runAntiCheatTests } from './tests/19-anti-cheat-deep.test.mjs'
import { runErrorProneTests } from './tests/20-error-prone.test.mjs'
import { runRegressionTests } from './tests/21-regression.test.mjs'
import { runStressTests } from './tests/22-stress-scenarios.test.mjs'
import { runApiContractTests } from './tests/23-api-contract.test.mjs'

async function main() {
  const startTime = Date.now()

  console.log('╔═══════════════════════════════════════════════════════╗')
  console.log('║  BanTayVang - Full Integration Test Suite             ║')
  console.log('║  Target: https://localhost:7249                       ║')
  console.log('║  23 Test Suites | SOLID + OWASP + E2E Coverage       ║')
  console.log('╚═══════════════════════════════════════════════════════╝')

  const suiteResults = []

  suiteResults.push(await runAuthTests())
  suiteResults.push(await runUserTests())
  suiteResults.push(await runCategoryTests())
  suiteResults.push(await runQuestionTests())
  suiteResults.push(await runExamTests())
  suiteResults.push(await runExamTakingTests())
  suiteResults.push(await runGradingTests())
  suiteResults.push(await runKyThiTests())
  suiteResults.push(await runNotificationTests())
  suiteResults.push(await runStatisticsTests())
  suiteResults.push(await runAuditLogTests())
  suiteResults.push(await runSecurityTests())
  suiteResults.push(await runEdgeCaseTests())
  suiteResults.push(await runDataIntegrityTests())
  suiteResults.push(await runPerformanceTests())
  suiteResults.push(await runExamWorkflowTests())
  suiteResults.push(await runAdvancedSecurityTests())
  suiteResults.push(await runAdvancedExamTests())
  suiteResults.push(await runAntiCheatTests())
  suiteResults.push(await runErrorProneTests())
  suiteResults.push(await runRegressionTests())
  suiteResults.push(await runStressTests())
  suiteResults.push(await runApiContractTests())

  const totalPassed = suiteResults.reduce((sum, r) => sum + r.passed, 0)
  const totalFailed = suiteResults.reduce((sum, r) => sum + r.failed, 0)
  const duration = ((Date.now() - startTime) / 1000).toFixed(1)

  console.log('\n')
  console.log('╔═══════════════════════════════════════════════════════╗')
  console.log(`║  FINAL RESULTS                                        ║`)
  console.log(`║  Total: ${totalPassed + totalFailed} tests | ✅ ${totalPassed} passed | ❌ ${totalFailed} failed  ║`)
  console.log(`║  Duration: ${duration}s                                       ║`)
  console.log('╚═══════════════════════════════════════════════════════╝')

  if (totalFailed > 0) {
    console.log(`\n⚠️  ${totalFailed} test(s) failed. Review output above for details.`)
  } else {
    console.log('\n🎉 All tests passed!')
  }

  process.exit(totalFailed > 0 ? 1 : 0)
}

main().catch((err) => {
  console.error('Fatal error:', err.message)
  process.exit(1)
})
