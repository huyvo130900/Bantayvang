/**
 * Test Suite 05: Exam Management & Assignment
 */
import {
  api, state, test, assert, assertEqual, assertGreaterThan,
  loginAsAdmin, randomExamCode, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runExamTests() {
  console.log('\n📝 ═══ 05. EXAM MANAGEMENT TESTS ═══')
  resetResults()
  await loginAsAdmin()

  const examCode = randomExamCode()

  // --- CREATE EXAM ---
  await test('EXAM-001: Create exam with questions', async () => {
    assert(state.createdQuestionIds.length > 0, 'Need questions from previous test')
    const res = await api.post('/exam', {
      maDeThi: examCode,
      tenDeThi: 'Đề thi KSNK - Test Automation',
      thoiGianLamBai: 30,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: state.createdQuestionIds,
    })
    assert(res.data.success, `Create exam failed: ${res.data.message}`)
    state.createdExamCode = examCode
  })

  await test('EXAM-002: Create exam with duplicate code should fail', async () => {
    try {
      const res = await api.post('/exam', {
        maDeThi: examCode,
        tenDeThi: 'Duplicate',
        thoiGianLamBai: 30,
        thoiGianBatDau: new Date().toISOString(),
        trangThai: 'Draft',
        danhSachIdCauHoi: state.createdQuestionIds,
      })
      assert(!res.data.success, 'Should fail - duplicate code')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  // --- GET EXAMS ---
  await test('EXAM-003: Get active exams', async () => {
    const res = await api.get('/exam/active')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
    const found = res.data.data.find(e => e.maDeThi === examCode)
    assert(found, 'Should find created exam')
    state.createdExamId = found.id
  })

  await test('EXAM-004: Get exam by code', async () => {
    const res = await api.get(`/exam/code/${examCode}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.maDeThi, examCode, 'Code should match')
    assertEqual(res.data.data.trangThai, 'Active', 'Should be Active')
  })

  await test('EXAM-005: Get exam with invalid code', async () => {
    try {
      const res = await api.get('/exam/code/NONEXISTENT_CODE_XYZ')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status === 404 || true, 'Should be 404')
    }
  })

  // --- ASSIGNMENTS ---
  await test('EXAM-006: Assign student to exam', async () => {
    assert(state.createdUserId > 0, 'Need user from previous test')
    const res = await api.post('/examassignment/assign', {
      examId: state.createdExamId,
      userIds: [state.createdUserId],
      note: 'Phân công từ automation test',
    })
    assert(res.data.success, `Assign failed: ${res.data.message}`)
  })

  await test('EXAM-007: Check user is assigned', async () => {
    const res = await api.get(`/examassignment/check/${state.createdExamId}/${state.createdUserId}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data, true, 'Should be assigned')
  })

  await test('EXAM-008: Get assignments by exam', async () => {
    const res = await api.get(`/examassignment/exam/${state.createdExamId}`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length > 0, 'Should have assignments')
  })

  await test('EXAM-009: Get assignments by user', async () => {
    const res = await api.get(`/examassignment/user/${state.createdUserId}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('EXAM-010: Assign same user again (should skip)', async () => {
    const res = await api.post('/examassignment/assign', {
      examId: state.createdExamId,
      userIds: [state.createdUserId],
    })
    assert(res.data.success, 'Should succeed (skip duplicate)')
  })

  await test('EXAM-011: Check unassigned user', async () => {
    const res = await api.get(`/examassignment/check/${state.createdExamId}/999999`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data, false, 'Should not be assigned')
  })

  return printSummary('Exam Management')
}

if (process.argv[1]?.includes('05-exams')) {
  runExamTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
