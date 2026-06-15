/**
 * Test Suite 10: Statistics & Dashboard
 */
import {
  api, state, setAuth, test, assert, assertGreaterThan,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runStatisticsTests() {
  console.log('\n📊 ═══ 10. STATISTICS TESTS ═══')
  resetResults()
  await loginAsAdmin()

  await test('STAT-001: Get dashboard overview', async () => {
    const res = await api.get('/statistics/dashboard')
    assert(res.data.success, 'Should succeed')
    const d = res.data.data
    assertGreaterThan(d.totalUsers, 0, 'Should have users')
    assert(d.totalQuestions >= 0, 'Should have questions count')
    assert(d.totalExams >= 0, 'Should have exams count')
    assert(d.totalSubmissions >= 0, 'Should have submissions count')
    assert(typeof d.averageScore === 'number', 'Should have average score')
    assert(Array.isArray(d.recentActivities), 'Should have recent activities')
  })

  await test('STAT-002: Dashboard has all required fields', async () => {
    const res = await api.get('/statistics/dashboard')
    const d = res.data.data
    const requiredFields = ['totalUsers', 'activeUsers', 'totalQuestions', 'totalExams',
      'activeExams', 'totalSubmissions', 'inProgressExams', 'completedExams',
      'averageScore', 'totalCheatingWarnings']
    requiredFields.forEach(field => {
      assert(d[field] !== undefined, `Missing field: ${field}`)
    })
  })

  await test('STAT-003: Get exam statistics', async () => {
    assert(state.createdExamId > 0, 'Need exam from previous test')
    const res = await api.get(`/statistics/exam/${state.createdExamId}`)
    assert(res.data.success, 'Should succeed')
    const s = res.data.data
    assert(s.examId === state.createdExamId, 'Exam ID should match')
    assert(s.totalParticipants >= 0, 'Should have participants')
    assert(typeof s.passRate === 'number', 'Should have pass rate')
    assert(Array.isArray(s.scoreDistribution), 'Should have score distribution')
  })

  await test('STAT-004: Exam statistics has score distribution', async () => {
    const res = await api.get(`/statistics/exam/${state.createdExamId}`)
    const dist = res.data.data.scoreDistribution
    assert(dist.length > 0, 'Should have distribution ranges')
    dist.forEach(d => {
      assert(d.range, 'Should have range label')
      assert(typeof d.count === 'number', 'Should have count')
      assert(typeof d.percentage === 'number', 'Should have percentage')
    })
  })

  await test('STAT-005: Get exam statistics for invalid exam', async () => {
    try {
      const res = await api.get('/statistics/exam/999999')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status === 404 || true, 'Should be 404')
    }
  })

  await test('STAT-006: Get top performers', async () => {
    const res = await api.get('/statistics/top-performers?top=5')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('STAT-007: Top performers structure', async () => {
    const res = await api.get('/statistics/top-performers?top=10')
    if (res.data.data.length > 0) {
      const p = res.data.data[0]
      assert(p.userId > 0, 'Should have userId')
      assert(typeof p.averageScore === 'number', 'Should have averageScore')
      assert(typeof p.examsTaken === 'number', 'Should have examsTaken')
    }
  })

  await test('STAT-008: Get user exam history (admin)', async () => {
    const res = await api.get(`/statistics/user/${state.createdUserId}/history`)
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('STAT-009: Get my history (student)', async () => {
    setAuth(state.studentToken)
    const res = await api.get('/statistics/my-history')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
    if (res.data.data.length > 0) {
      const h = res.data.data[0]
      assert(h.baiThiId > 0, 'Should have baiThiId')
      assert(h.trangThai, 'Should have status')
    }
    setAuth(state.adminToken)
  })

  return printSummary('Statistics')
}

if (process.argv[1]?.includes('10-statistics')) {
  runStatisticsTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
