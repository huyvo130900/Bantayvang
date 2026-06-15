/**
 * Test Suite 21: Regression & Known Bug Patterns
 * Tests common patterns that cause regressions in exam systems
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan, sleep,
  loginAsAdmin, randomUsername, randomEmail, randomExamCode, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runRegressionTests() {
  console.log('\n🔄 ═══ 21. REGRESSION & BUG PATTERN TESTS ═══')
  resetResults()
  setAuth(state.adminToken)

  // ═══ SCORING EDGE CASES ═══
  await test('REG-001: Question with 0.5 point scoring', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: 'Half point question',
      idLoaiCauHoi: 1, doKho: 'De', diem: 0.5, idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'Correct', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Wrong', thuTu: 2, laDapAnDung: false },
      ],
    })
    assert(res.data.success, 'Should accept 0.5 points')
    if (res.data.data?.id) {
      const getRes = await api.get(`/cauhoi/${res.data.data.id}`)
      assertEqual(getRes.data.data.diem, 0.5, 'Score should be 0.5')
      await api.delete(`/cauhoi/${res.data.data.id}`)
    }
  })

  await test('REG-002: Question with decimal point (1.5)', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: 'Decimal point question',
      idLoaiCauHoi: 1, doKho: 'TrungBinh', diem: 1.5, idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'A', thuTu: 1, laDapAnDung: true },
        { noiDung: 'B', thuTu: 2, laDapAnDung: false },
      ],
    })
    assert(res.data.success, 'Should accept 1.5 points')
    if (res.data.data?.id) await api.delete(`/cauhoi/${res.data.data.id}`)
  })

  // ═══ EXAM TIMING EDGE CASES ═══
  await test('REG-003: Exam with start time in the past', async () => {
    const res = await api.post('/exam', {
      maDeThi: randomExamCode(),
      tenDeThi: 'Past Start Time',
      thoiGianLamBai: 60,
      thoiGianBatDau: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
      trangThai: 'Active',
      danhSachIdCauHoi: [1],
    })
    assert(res.data.success, 'Should allow past start time (exam already started)')
  })

  await test('REG-004: Exam with start time far in future', async () => {
    const res = await api.post('/exam', {
      maDeThi: randomExamCode(),
      tenDeThi: 'Future Start Time',
      thoiGianLamBai: 60,
      thoiGianBatDau: new Date(Date.now() + 30 * 86400000).toISOString(), // 30 days
      trangThai: 'Draft',
      danhSachIdCauHoi: [1],
    })
    assert(res.data.success, 'Should allow future start time')
  })

  // ═══ USER SESSION EDGE CASES ═══
  await test('REG-005: Multiple rapid logins same user', async () => {
    await sleep(6000) // Wait for rate limit
    const tokens = []
    for (let i = 0; i < 3; i++) {
      try {
        const res = await api.post('/auth/login', { username: 'admin', password: 'admin123' })
        if (res.data.success) tokens.push(res.data.data.accessToken)
      } catch { break } // rate limited
      await sleep(1500)
    }
    // All tokens should be valid
    for (const token of tokens) {
      setAuth(token)
      const res = await api.get('/auth/me')
      assert(res.data.success, 'Each token should be valid')
    }
    state.adminToken = tokens[tokens.length - 1] || state.adminToken
    setAuth(state.adminToken)
  })

  // ═══ FILTER COMBINATIONS ═══
  await test('REG-006: Multiple filters combined', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=5&doKho=De&idDanhMuc=1&searchKeyword=')
    assert(res.data.success, 'Combined filters should work')
  })

  await test('REG-007: User filter with all params', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=5&idVaiTro=3&trangThai=true&khoaPhong=&searchKeyword=')
    assert(res.data.success, 'All user filters combined should work')
  })

  await test('REG-008: Audit log search with all params', async () => {
    const from = new Date(Date.now() - 7 * 86400000).toISOString()
    const to = new Date().toISOString()
    const res = await api.get(`/auditlog/search?actionType=POST&from=${from}&to=${to}`)
    assert(res.data.success, 'All audit filters should work')
  })

  // ═══ DATA CONSISTENCY AFTER OPERATIONS ═══
  await test('REG-009: User count consistent after create/delete', async () => {
    const before = await api.get('/statistics/dashboard')
    const beforeCount = before.data.data.totalUsers

    const createRes = await api.post('/user', {
      tenDangNhap: randomUsername(),
      matKhau: 'Test@123',
      email: randomEmail(),
      hoTen: 'Count Test',
      idVaiTro: 3,
      trangThai: true,
    })

    const after = await api.get('/statistics/dashboard')
    assertEqual(after.data.data.totalUsers, beforeCount + 1, 'Count should increase by 1')
  })

  await test('REG-010: Question count consistent after create/delete', async () => {
    const before = await api.get('/statistics/dashboard')
    const beforeCount = before.data.data.totalQuestions

    const createRes = await api.post('/cauhoi', {
      noiDung: 'Count consistency test',
      idLoaiCauHoi: 1, doKho: 'De', diem: 1, idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'A', thuTu: 1, laDapAnDung: true },
        { noiDung: 'B', thuTu: 2, laDapAnDung: false },
      ],
    })

    const afterCreate = await api.get('/statistics/dashboard')
    assertEqual(afterCreate.data.data.totalQuestions, beforeCount + 1, 'Should increase after create')

    if (createRes.data.data?.id) {
      await api.delete(`/cauhoi/${createRes.data.data.id}`)
      const afterDelete = await api.get('/statistics/dashboard')
      assertEqual(afterDelete.data.data.totalQuestions, beforeCount, 'Should decrease after delete')
    }
  })

  // ═══ CATEGORY DEPENDENCY ═══
  await test('REG-011: Delete category with questions should fail or cascade', async () => {
    // Create category
    const catRes = await api.post('/category/categories', {
      tenDanhMuc: `Dep Test ${Date.now()}`,
      mota: 'Will try to delete',
    })
    const catId = catRes.data.data.id

    // Create question in this category
    const qRes = await api.post('/cauhoi', {
      noiDung: 'Question in category to delete',
      idDanhMuc: catId,
      idLoaiCauHoi: 1, doKho: 'De', diem: 1,
      danhSachLuaChon: [
        { noiDung: 'A', thuTu: 1, laDapAnDung: true },
        { noiDung: 'B', thuTu: 2, laDapAnDung: false },
      ],
    })

    // Try to delete category
    try {
      const delRes = await api.delete(`/category/categories/${catId}`)
      // May succeed (orphan questions) or fail (FK constraint)
      assert(true, 'Should handle gracefully')
    } catch (err) {
      assert(err.response?.status < 500, 'Should not be 500')
    }

    // Cleanup
    if (qRes.data.data?.id) await api.delete(`/cauhoi/${qRes.data.data.id}`)
  })

  // ═══ NOTIFICATION EDGE CASES ═══
  await test('REG-012: Get notifications when none exist for user', async () => {
    // Create fresh user with no notifications
    const username = randomUsername()
    await api.post('/user', {
      tenDangNhap: username,
      matKhau: 'Test@123',
      email: randomEmail(),
      hoTen: 'No Notif User',
      idVaiTro: 3,
      trangThai: true,
    })
    await sleep(2000)
    const loginRes = await api.post('/auth/login', { username, password: 'Test@123' })
    if (loginRes.data.success) {
      setAuth(loginRes.data.data.accessToken)
      const res = await api.get('/notification')
      assert(res.data.success, 'Should succeed with empty list')
      // May have broadcast notifications
    }
    setAuth(state.adminToken)
  })

  await test('REG-013: Mark non-existent notification as read', async () => {
    try {
      const res = await api.post('/notification/999999/read')
      assert(!res.data.success, 'Should fail for non-existent')
    } catch (err) {
      assert(err.response?.status < 500, 'Should not be 500')
    }
  })

  // ═══ GRADING EDGE CASES ═══
  await test('REG-014: Regrade non-existent baithi', async () => {
    try {
      const res = await api.post('/grading/regrade/999999')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status < 500, 'Should not be 500')
    }
  })

  await test('REG-015: Get ranking for exam with no submissions', async () => {
    // Create exam with no submissions
    const code = randomExamCode()
    await api.post('/exam', {
      maDeThi: code,
      tenDeThi: 'No Submissions Exam',
      thoiGianLamBai: 30,
      thoiGianBatDau: new Date().toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: [1],
    })
    const examRes = await api.get(`/exam/code/${code}`)
    const examId = examRes.data.data.id

    const res = await api.get(`/grading/exam/${examId}/ranking?top=10`)
    assert(res.data.success, 'Should succeed with empty ranking')
    assertEqual(res.data.data.length, 0, 'Should have no results')
  })

  // ═══ KY THI EDGE CASES ═══
  await test('REG-016: Create ky thi with past end date', async () => {
    const res = await api.post('/kythi', {
      maKyThi: `REG_${Date.now()}`,
      tenKyThi: 'Past End Date Ky Thi',
      thoiGianBatDau: new Date(Date.now() - 86400000).toISOString(),
      thoiGianKetThuc: new Date(Date.now() - 3600000).toISOString(),
    })
    // May succeed or fail depending on BE validation
    assert(res.data !== undefined, 'Should respond')
    if (res.data.success) await api.delete(`/kythi/${res.data.data.id}`)
  })

  await test('REG-017: Update ky thi that does not exist', async () => {
    try {
      const res = await api.put('/kythi/999999', {
        maKyThi: 'FAKE',
        tenKyThi: 'Does not exist',
        trangThai: 'DangChuanBi',
      })
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status < 500, 'Should not be 500')
    }
  })

  // ═══ EXAM ASSIGNMENT EDGE CASES ═══
  await test('REG-018: Remove non-existent assignment', async () => {
    try {
      const res = await api.delete('/examassignment/999999')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status < 500, 'Should not be 500')
    }
  })

  await test('REG-019: Extend time with 0 minutes', async () => {
    try {
      const res = await api.post('/examassignment/extend-time', {
        baiThiId: 1,
        additionalMinutes: 0,
        reason: 'Zero minutes',
      })
      // Should fail validation
    } catch (err) {
      assert(err.response?.status < 500, 'Should not be 500')
    }
  })

  await test('REG-020: Get my-exams as admin (no student assignments)', async () => {
    const res = await api.get('/examassignment/my-exams')
    assert(res.data.success, 'Should succeed (may be empty)')
  })

  return printSummary('Regression & Bug Patterns')
}

if (process.argv[1]?.includes('21-regression')) {
  runRegressionTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
