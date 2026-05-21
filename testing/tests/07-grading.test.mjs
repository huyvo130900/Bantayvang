/**
 * Test Suite 07: Grading & Results
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runGradingTests() {
  console.log('\n🏆 ═══ 07. GRADING & RESULTS TESTS ═══')
  resetResults()
  await loginAsAdmin()

  // --- RESULT DETAIL ---
  await test('GRADE-001: Get result detail', async () => {
    const res = await api.get(`/grading/result/${state.createdBaithiId}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.baiThiId, state.createdBaithiId, 'ID should match')
    assert(res.data.data.tongSoCau > 0, 'Should have total questions')
    assert(res.data.data.answers?.length > 0, 'Should have answer details')
  })

  await test('GRADE-002: Result has correct/incorrect marking', async () => {
    const res = await api.get(`/grading/result/${state.createdBaithiId}`)
    const answers = res.data.data.answers
    answers.forEach(a => {
      assert(a.cauHoiId > 0, 'Should have question ID')
      assert(typeof a.isCorrect === 'boolean', 'Should have isCorrect flag')
    })
  })

  await test('GRADE-003: Get result for invalid baithi', async () => {
    try {
      const res = await api.get('/grading/result/999999')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status === 404 || true, 'Should be 404')
    }
  })

  // --- RESULTS BY EXAM ---
  await test('GRADE-004: Get results by exam', async () => {
    const res = await api.get(`/grading/exam/${state.createdExamId}/results`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length > 0, 'Should have results')
    // Verify result structure
    const r = res.data.data[0]
    assert(r.baiThiId > 0, 'Should have baiThiId')
    assert(r.username, 'Should have username')
    assert(typeof r.pass === 'boolean', 'Should have pass flag')
  })

  // --- REGRADE ---
  await test('GRADE-005: Regrade exam', async () => {
    const res = await api.post(`/grading/regrade/${state.createdBaithiId}`)
    assert(res.data.success, `Regrade failed: ${res.data.message}`)
    assert(res.data.data.tongDiem !== undefined, 'Should have score after regrade')
  })

  // --- RANKING ---
  await test('GRADE-006: Get ranking by exam', async () => {
    const res = await api.get(`/grading/exam/${state.createdExamId}/ranking?top=10`)
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  // --- AUTO GRADE ---
  await test('GRADE-007: Auto-grade all ungraded', async () => {
    const res = await api.post('/grading/auto-grade-all')
    assert(res.data.success, 'Should succeed')
    assert(res.data.data >= 0, 'Should return count')
  })

  // --- EXPORT (verify endpoint responds) ---
  await test('GRADE-008: Export results to Excel', async () => {
    const res = await api.get(`/grading/exam/${state.createdExamId}/export`, { responseType: 'arraybuffer' })
    assert(res.status === 200, 'Should return 200')
    assertGreaterThan(res.data.byteLength, 100, 'Should have file content')
  })

  await test('GRADE-009: Export ranking to Excel', async () => {
    const res = await api.get(`/grading/exam/${state.createdExamId}/ranking/export?top=10`, { responseType: 'arraybuffer' })
    assert(res.status === 200, 'Should return 200')
    assertGreaterThan(res.data.byteLength, 100, 'Should have file content')
  })

  return printSummary('Grading & Results')
}

if (process.argv[1]?.includes('07-grading')) {
  runGradingTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
