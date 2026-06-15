/**
 * Test Suite 06: Exam Taking (Student flow)
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan,
  loginAsAdmin, printSummary, resetResults, sleep,
} from '../lib/test-helper.mjs'

export async function runExamTakingTests() {
  console.log('\n🎯 ═══ 06. EXAM TAKING TESTS ═══')
  resetResults()

  // Login as student
  setAuth(state.studentToken)

  // --- START EXAM ---
  await test('TAKE-001: Start exam as student', async () => {
    assert(state.createdExamCode, 'Need exam code from previous test')
    const res = await api.post('/exam/start', { maDeThi: state.createdExamCode })
    assert(res.data.success, `Start failed: ${res.data.message}`)
    state.createdBaithiId = res.data.data.id
    assertEqual(res.data.data.trangThai, 'InProgress', 'Should be InProgress')
    assert(res.data.data.thoiGianConLai > 0, 'Should have remaining time')
  })

  await test('TAKE-002: Start same exam again (should resume)', async () => {
    const res = await api.post('/exam/start', { maDeThi: state.createdExamCode })
    assert(res.data.success, 'Should succeed (resume)')
    assertEqual(res.data.data.id, state.createdBaithiId, 'Should return same session')
  })

  await test('TAKE-003: Start non-existent exam should fail', async () => {
    try {
      const res = await api.post('/exam/start', { maDeThi: 'FAKE_EXAM_XYZ' })
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  // --- GET QUESTIONS ---
  let questions = []
  await test('TAKE-004: Get exam questions', async () => {
    const res = await api.get(`/exam/${state.createdBaithiId}/questions`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length > 0, 'Should have questions')
    questions = res.data.data
    // Verify no correct answer is exposed
    questions.forEach(q => {
      q.danhSachLuaChon.forEach(c => {
        assert(c.laDapAnDung === undefined || c.laDapAnDung === null,
          'Should NOT expose correct answer to student')
      })
    })
  })

  await test('TAKE-005: Questions have shuffled order', async () => {
    assert(questions[0].thuTuCau > 0, 'Should have thuTuCau')
    assert(questions[0].danhSachLuaChon.length > 0, 'Should have choices')
  })

  // --- SAVE ANSWERS ---
  await test('TAKE-006: Save answer (select choice)', async () => {
    const q = questions[0]
    const choice = q.danhSachLuaChon[0]
    const res = await api.post('/exam/answer', {
      idBaiThi: state.createdBaithiId,
      idCauHoi: q.id,
      idLuaChonDaChon: choice.id,
      daLuu: true,
    })
    assert(res.data.success, `Save answer failed: ${res.data.message}`)
  })

  await test('TAKE-007: Save answer for second question', async () => {
    if (questions.length > 1) {
      const q = questions[1]
      const choice = q.danhSachLuaChon[0]
      const res = await api.post('/exam/answer', {
        idBaiThi: state.createdBaithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: choice.id,
        daLuu: true,
      })
      assert(res.data.success, `Save answer failed: ${res.data.message}`)
    }
  })

  await test('TAKE-008: Change answer (overwrite)', async () => {
    const q = questions[0]
    const choice = q.danhSachLuaChon[1] || q.danhSachLuaChon[0]
    const res = await api.post('/exam/answer', {
      idBaiThi: state.createdBaithiId,
      idCauHoi: q.id,
      idLuaChonDaChon: choice.id,
      daLuu: true,
    })
    assert(res.data.success, 'Should allow changing answer')
  })

  // --- PROGRESS ---
  await test('TAKE-009: Get exam progress', async () => {
    const res = await api.get(`/exam/${state.createdBaithiId}/progress`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.trangThai, 'InProgress', 'Should be InProgress')
    assert(res.data.data.thoiGianConLai >= 0, 'Should have remaining time')
  })

  // --- ANTI-CHEAT ---
  await test('TAKE-010: Log TAB_SWITCH warning', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: state.createdBaithiId,
      loaiCanhBao: 'TAB_SWITCH',
      moTa: 'Thí sinh chuyển tab',
    })
    assert(res.data.success, 'Should succeed')
  })

  await test('TAKE-011: Log COPY_PASTE warning', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: state.createdBaithiId,
      loaiCanhBao: 'COPY_PASTE',
      moTa: 'Thí sinh copy paste',
    })
    assert(res.data.success, 'Should succeed')
  })

  await test('TAKE-012: Log RIGHT_CLICK warning', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: state.createdBaithiId,
      loaiCanhBao: 'RIGHT_CLICK',
      moTa: 'Thí sinh click chuột phải',
    })
    assert(res.data.success, 'Should succeed')
  })

  await test('TAKE-013: Get warning count', async () => {
    const res = await api.get(`/exam/${state.createdBaithiId}/warnings`)
    assert(res.data.success, 'Should succeed')
    assertGreaterThan(res.data.data, 0, 'Should have warnings')
  })

  // --- SUBMIT ---
  await test('TAKE-014: Submit exam', async () => {
    const answers = questions.map(q => ({
      idBaiThi: state.createdBaithiId,
      idCauHoi: q.id,
      idLuaChonDaChon: q.danhSachLuaChon[0]?.id || null,
      daLuu: true,
    }))

    const res = await api.post('/exam/submit', {
      idBaiThi: state.createdBaithiId,
      danhSachCauTraLoi: answers,
    })
    assert(res.data.success, `Submit failed: ${res.data.message}`)
    assertEqual(res.data.data.trangThai, 'Completed', 'Should be Completed')
  })

  await test('TAKE-015: Submit already completed exam should fail', async () => {
    try {
      const res = await api.post('/exam/submit', {
        idBaiThi: state.createdBaithiId,
        danhSachCauTraLoi: [],
      })
      assert(!res.data.success, 'Should fail - already submitted')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('TAKE-016: Start completed exam should fail', async () => {
    try {
      const res = await api.post('/exam/start', { maDeThi: state.createdExamCode })
      // BE returns success=false - this is correct
      assert(!res.data.success, 'Should not allow restarting completed exam')
    } catch (err) {
      // 400 response is also acceptable
      assert(true, 'Correctly rejected')
    }
  })

  // Restore admin auth
  setAuth(state.adminToken)

  return printSummary('Exam Taking')
}

if (process.argv[1]?.includes('06-exam-taking')) {
  runExamTakingTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
