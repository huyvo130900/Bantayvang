/**
 * Test Suite 22: Stress & Chaos Scenarios
 * Tests system behavior under unusual load patterns and chaotic inputs
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan, sleep,
  loginAsAdmin, randomUsername, randomEmail, randomExamCode, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runStressTests() {
  console.log('\n🔥 ═══ 22. STRESS & CHAOS SCENARIOS ═══')
  resetResults()
  setAuth(state.adminToken)

  // ═══ RAPID FIRE OPERATIONS ═══
  await test('STRESS-001: Create 10 users rapidly', async () => {
    const promises = Array.from({ length: 10 }, () =>
      api.post('/user', {
        tenDangNhap: randomUsername(),
        matKhau: 'Stress@123',
        email: randomEmail(),
        hoTen: 'Stress User',
        idVaiTro: 3,
        trangThai: true,
      })
    )
    const results = await Promise.all(promises)
    const successCount = results.filter(r => r.data.success).length
    assertGreaterThan(successCount, 7, `At least 8/10 should succeed, got ${successCount}`)
  })

  await test('STRESS-002: Create 10 notifications rapidly', async () => {
    const promises = Array.from({ length: 10 }, (_, i) =>
      api.post('/notification', {
        userId: 1,
        title: `Stress Notification ${i}`,
        message: `Rapid fire notification ${i}`,
        type: 'Info',
      })
    )
    const results = await Promise.all(promises)
    const successCount = results.filter(r => r.data.success).length
    assertEqual(successCount, 10, 'All 10 notifications should succeed')
  })

  await test('STRESS-003: 20 concurrent GET requests to different endpoints', async () => {
    const endpoints = [
      '/category/categories', '/category/types',
      '/cauhoi?pageNumber=1&pageSize=5', '/cauhoi?pageNumber=2&pageSize=5',
      '/exam/active', '/notification/unread-count',
      '/statistics/dashboard', '/kythi',
      '/notification/upcoming-exams', '/notification/current-exams',
      '/auditlog/recent?top=10', '/user?pageNumber=1&pageSize=5',
      '/statistics/top-performers?top=5',
      '/category/categories', '/category/types',
      '/cauhoi?pageNumber=1&pageSize=10', '/exam/active',
      '/notification', '/kythi', '/auditlog/recent?top=5',
    ]
    const start = Date.now()
    const results = await Promise.all(endpoints.map(e => api.get(e)))
    const duration = Date.now() - start
    const allOk = results.every(r => r.status === 200)
    assert(allOk, 'All 20 concurrent GETs should return 200')
    assert(duration < 10000, `Should complete in <10s, took ${duration}ms`)
  })

  // ═══ RAPID ANSWER SAVING (simulating fast clicking) ═══
  await test('STRESS-004: Rapid answer saves (simulate fast clicking)', async () => {
    // Create a quick exam for this test
    const qIds = []
    for (let i = 0; i < 3; i++) {
      const r = await api.post('/cauhoi', {
        noiDung: `Stress answer Q${i} ${Date.now()}`,
        idLoaiCauHoi: 1, doKho: 'De', diem: 1, idDanhMuc: 1,
        danhSachLuaChon: [
          { noiDung: 'A', thuTu: 1, laDapAnDung: true },
          { noiDung: 'B', thuTu: 2, laDapAnDung: false },
        ],
      })
      if (r.data.success) qIds.push(r.data.data.id)
    }

    const code = randomExamCode()
    await api.post('/exam', {
      maDeThi: code,
      tenDeThi: 'Stress Answer Test',
      thoiGianLamBai: 30,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: qIds,
    })

    const examRes = await api.get(`/exam/code/${code}`)
    const examId = examRes.data.data.id

    // Create student and start
    const username = randomUsername()
    await api.post('/user', {
      tenDangNhap: username, matKhau: 'Stress@123',
      email: randomEmail(), hoTen: 'Stress Student', idVaiTro: 3, trangThai: true,
    })
    const userListRes = await api.get(`/user?pageNumber=1&pageSize=100&searchKeyword=${username}`)
    const userId = userListRes.data.data.find(u => u.tenDangNhap === username)?.id
    await api.post('/examassignment/assign', { examId, userIds: [userId] })

    await sleep(2000)
    const loginRes = await api.post('/auth/login', { username, password: 'Stress@123' })
    setAuth(loginRes.data.data.accessToken)

    const startRes = await api.post('/exam/start', { maDeThi: code })
    const baithiId = startRes.data.data.id

    const questionsRes = await api.get(`/exam/${baithiId}/questions`)
    const questions = questionsRes.data.data

    // Rapid fire: save answer 5 times for same question (simulating clicking different options)
    const q = questions[0]
    const savePromises = q.danhSachLuaChon.map(choice =>
      api.post('/exam/answer', {
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: choice.id,
        daLuu: true,
      })
    )
    const saveResults = await Promise.all(savePromises)
    const saveSuccess = saveResults.filter(r => r.data.success).length
    assertGreaterThan(saveSuccess, 0, 'At least some saves should succeed')

    // Submit
    await api.post('/exam/submit', {
      idBaiThi: baithiId,
      danhSachCauTraLoi: questions.map(qq => ({
        idBaiThi: baithiId,
        idCauHoi: qq.id,
        idLuaChonDaChon: qq.danhSachLuaChon[0].id,
        daLuu: true,
      })),
    })

    setAuth(state.adminToken)
    for (const id of qIds) await api.delete(`/cauhoi/${id}`)
  })

  // ═══ LARGE DATA RETRIEVAL ═══
  await test('STRESS-005: Get all users (large list)', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=100')
    assert(res.data.success, 'Should handle large user list')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('STRESS-006: Get all questions (large list)', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=100')
    assert(res.data.success, 'Should handle large question list')
  })

  await test('STRESS-007: Get audit log (200 entries)', async () => {
    const start = Date.now()
    const res = await api.get('/auditlog/recent?top=200')
    const duration = Date.now() - start
    assert(res.data.success, 'Should succeed')
    assert(duration < 3000, `Should be fast, took ${duration}ms`)
  })

  // ═══ MIXED OPERATIONS ═══
  await test('STRESS-008: Interleaved read/write operations', async () => {
    const ops = [
      api.get('/category/categories'),
      api.post('/notification', { userId: 1, title: 'Interleave', message: 'test', type: 'Info' }),
      api.get('/statistics/dashboard'),
      api.get('/cauhoi?pageNumber=1&pageSize=5'),
      api.post('/notification', { userId: 1, title: 'Interleave2', message: 'test2', type: 'Info' }),
      api.get('/exam/active'),
      api.get('/notification/unread-count'),
      api.get('/kythi'),
    ]
    const results = await Promise.all(ops)
    const allOk = results.every(r => r.status === 200)
    assert(allOk, 'All interleaved operations should succeed')
  })

  // ═══ REPEATED OPERATIONS ═══
  await test('STRESS-009: Read same resource 20 times', async () => {
    const promises = Array.from({ length: 20 }, () => api.get('/statistics/dashboard'))
    const results = await Promise.all(promises)
    const allSuccess = results.every(r => r.data.success)
    assert(allSuccess, 'All 20 reads should succeed')
  })

  await test('STRESS-010: Mark all read multiple times', async () => {
    for (let i = 0; i < 5; i++) {
      const res = await api.post('/notification/mark-all-read')
      assert(res.data.success, `Iteration ${i} should succeed`)
    }
  })

  return printSummary('Stress & Chaos')
}

if (process.argv[1]?.includes('22-stress')) {
  runStressTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
