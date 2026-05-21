/**
 * Test Suite 15: Performance & Load Testing
 * Tests response times, bulk operations, and system under load
 */
import {
  api, state, setAuth, test, assert, assertGreaterThan,
  loginAsAdmin, randomUsername, randomEmail, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runPerformanceTests() {
  console.log('\n⚡ ═══ 15. PERFORMANCE TESTS ═══')
  resetResults()
  setAuth(state.adminToken)

  // --- RESPONSE TIME ---
  await test('PERF-001: Dashboard loads under 2s', async () => {
    const start = Date.now()
    await api.get('/statistics/dashboard')
    const duration = Date.now() - start
    assert(duration < 2000, `Dashboard took ${duration}ms (max 2000ms)`)
  })

  await test('PERF-002: Question list loads under 1s', async () => {
    const start = Date.now()
    await api.get('/cauhoi?pageNumber=1&pageSize=20')
    const duration = Date.now() - start
    assert(duration < 1000, `Questions took ${duration}ms (max 1000ms)`)
  })

  await test('PERF-003: User list loads under 1s', async () => {
    const start = Date.now()
    await api.get('/user?pageNumber=1&pageSize=20')
    const duration = Date.now() - start
    assert(duration < 1000, `Users took ${duration}ms (max 1000ms)`)
  })

  await test('PERF-004: Active exams loads under 1s', async () => {
    const start = Date.now()
    await api.get('/exam/active')
    const duration = Date.now() - start
    assert(duration < 1000, `Exams took ${duration}ms (max 1000ms)`)
  })

  await test('PERF-005: Notifications load under 1s', async () => {
    const start = Date.now()
    await api.get('/notification')
    const duration = Date.now() - start
    assert(duration < 1000, `Notifications took ${duration}ms (max 1000ms)`)
  })

  await test('PERF-006: Audit log loads under 2s', async () => {
    const start = Date.now()
    await api.get('/auditlog/recent?top=200')
    const duration = Date.now() - start
    assert(duration < 2000, `Audit log took ${duration}ms (max 2000ms)`)
  })

  await test('PERF-007: Grading results load under 2s', async () => {
    if (!state.createdExamId) return
    const start = Date.now()
    await api.get(`/grading/exam/${state.createdExamId}/results`)
    const duration = Date.now() - start
    assert(duration < 2000, `Grading took ${duration}ms (max 2000ms)`)
  })

  // --- BULK OPERATIONS ---
  await test('PERF-008: Create 5 questions in sequence under 5s', async () => {
    const start = Date.now()
    const ids = []
    for (let i = 0; i < 5; i++) {
      const res = await api.post('/cauhoi', {
        noiDung: `Perf test question ${i + 1} - ${Date.now()}`,
        idLoaiCauHoi: 1, doKho: 'De', diem: 1, idDanhMuc: 1,
        danhSachLuaChon: [
          { noiDung: 'A', thuTu: 1, laDapAnDung: true },
          { noiDung: 'B', thuTu: 2, laDapAnDung: false },
        ],
      })
      if (res.data.success) ids.push(res.data.data.id)
    }
    const duration = Date.now() - start
    assert(duration < 5000, `Bulk create took ${duration}ms (max 5000ms)`)
    // Cleanup
    for (const id of ids) await api.delete(`/cauhoi/${id}`)
  })

  await test('PERF-009: Create 5 users in sequence under 5s', async () => {
    const start = Date.now()
    for (let i = 0; i < 5; i++) {
      await api.post('/user', {
        tenDangNhap: randomUsername(),
        matKhau: 'Perf@123',
        email: randomEmail(),
        hoTen: `Perf User ${i}`,
        idVaiTro: 3,
        trangThai: true,
      })
    }
    const duration = Date.now() - start
    assert(duration < 5000, `Bulk user create took ${duration}ms (max 5000ms)`)
  })

  // --- CONCURRENT REQUESTS ---
  await test('PERF-010: 10 concurrent GET requests', async () => {
    const start = Date.now()
    const promises = Array.from({ length: 10 }, (_, i) => {
      const endpoints = [
        '/category/categories',
        '/category/types',
        '/cauhoi?pageNumber=1&pageSize=5',
        '/exam/active',
        '/notification/unread-count',
        '/statistics/dashboard',
        '/kythi',
        '/notification/upcoming-exams',
        '/notification/current-exams',
        '/auditlog/recent?top=10',
      ]
      return api.get(endpoints[i])
    })
    const results = await Promise.all(promises)
    const duration = Date.now() - start
    const allSuccess = results.every(r => r.data.success)
    assert(allSuccess, 'All concurrent requests should succeed')
    assert(duration < 5000, `Concurrent requests took ${duration}ms (max 5000ms)`)
  })

  await test('PERF-011: 5 concurrent notification creates', async () => {
    const start = Date.now()
    const promises = Array.from({ length: 5 }, (_, i) =>
      api.post('/notification', {
        userId: 1,
        title: `Perf Notification ${i}`,
        message: `Performance test ${i}`,
        type: 'Info',
      })
    )
    const results = await Promise.all(promises)
    const duration = Date.now() - start
    const allSuccess = results.every(r => r.data.success)
    assert(allSuccess, 'All should succeed')
    assert(duration < 3000, `Concurrent creates took ${duration}ms (max 3000ms)`)
  })

  // --- LARGE DATA ---
  await test('PERF-012: Fetch large page (100 items) under 2s', async () => {
    const start = Date.now()
    await api.get('/cauhoi?pageNumber=1&pageSize=100')
    const duration = Date.now() - start
    assert(duration < 2000, `Large page took ${duration}ms (max 2000ms)`)
  })

  await test('PERF-013: Export Excel under 5s', async () => {
    if (!state.createdExamId) return
    const start = Date.now()
    await api.get(`/grading/exam/${state.createdExamId}/export`, { responseType: 'arraybuffer' })
    const duration = Date.now() - start
    assert(duration < 5000, `Export took ${duration}ms (max 5000ms)`)
  })

  return printSummary('Performance')
}

if (process.argv[1]?.includes('15-performance')) {
  runPerformanceTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
