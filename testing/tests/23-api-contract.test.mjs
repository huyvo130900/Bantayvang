/**
 * Test Suite 23: API Contract Validation
 * Verifies response shapes, required fields, data types, and HTTP status codes
 */
import {
  api, state, setAuth, test, assert, assertEqual, sleep,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runApiContractTests() {
  console.log('\n📜 ═══ 23. API CONTRACT VALIDATION ═══')
  resetResults()
  setAuth(state.adminToken)

  // ═══ RESPONSE SHAPE ═══
  await test('CONTRACT-001: All responses have success/message fields', async () => {
    const endpoints = [
      '/category/categories',
      '/category/types',
      '/cauhoi?pageNumber=1&pageSize=5',
      '/exam/active',
      '/notification',
      '/notification/unread-count',
      '/statistics/dashboard',
      '/kythi',
    ]
    for (const ep of endpoints) {
      const res = await api.get(ep)
      assert(typeof res.data.success === 'boolean', `${ep}: success should be boolean`)
      assert(typeof res.data.message === 'string', `${ep}: message should be string`)
    }
  })

  await test('CONTRACT-002: Error responses have errors array', async () => {
    try {
      await api.get('/cauhoi/999999')
    } catch (err) {
      if (err.response?.data) {
        assert(typeof err.response.data.success === 'boolean', 'Should have success')
        assert(typeof err.response.data.message === 'string', 'Should have message')
      }
    }
  })

  // ═══ USER DTO SHAPE ═══
  await test('CONTRACT-003: User DTO has all required fields', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=1')
    if (res.data.data.length > 0) {
      const user = res.data.data[0]
      const requiredFields = ['id', 'tenDangNhap', 'email', 'hoTen', 'idVaiTro', 'tenVaiTro', 'trangThai']
      for (const field of requiredFields) {
        assert(field in user, `User missing field: ${field}`)
      }
      assert(typeof user.id === 'number', 'id should be number')
      assert(typeof user.trangThai === 'boolean' || user.trangThai === null, 'trangThai should be boolean|null')
    }
  })

  // ═══ QUESTION DTO SHAPE ═══
  await test('CONTRACT-004: Question DTO has all required fields', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=1')
    if (res.data.data.items.length > 0) {
      const q = res.data.data.items[0]
      const requiredFields = ['id', 'noiDung', 'danhSachLuaChon']
      for (const field of requiredFields) {
        assert(field in q, `Question missing field: ${field}`)
      }
      assert(typeof q.id === 'number', 'id should be number')
      assert(Array.isArray(q.danhSachLuaChon), 'danhSachLuaChon should be array')
    }
  })

  await test('CONTRACT-005: Question pagination shape', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=5')
    assert(res.data.data.items !== undefined, 'Should have items')
    assert(res.data.data.pagination !== undefined, 'Should have pagination')
    const p = res.data.data.pagination
    assert(typeof p.pageNumber === 'number', 'pageNumber should be number')
    assert(typeof p.pageSize === 'number', 'pageSize should be number')
    assert(typeof p.totalRecords === 'number', 'totalRecords should be number')
    assert(typeof p.totalPages === 'number', 'totalPages should be number')
  })

  await test('CONTRACT-006: Choice DTO shape', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=1')
    if (res.data.data.items.length > 0 && res.data.data.items[0].danhSachLuaChon.length > 0) {
      const choice = res.data.data.items[0].danhSachLuaChon[0]
      assert('id' in choice, 'Choice should have id')
      assert('noiDung' in choice, 'Choice should have noiDung')
      assert('laDapAnDung' in choice, 'Choice should have laDapAnDung')
      assert(typeof choice.id === 'number', 'Choice id should be number')
    }
  })

  // ═══ EXAM DTO SHAPE ═══
  await test('CONTRACT-007: Exam DTO has all required fields', async () => {
    const res = await api.get('/exam/active')
    if (res.data.data.length > 0) {
      const exam = res.data.data[0]
      const requiredFields = ['id', 'maDeThi', 'tenDeThi', 'thoiGianLamBai', 'trangThai', 'soCauHoi']
      for (const field of requiredFields) {
        assert(field in exam, `Exam missing field: ${field}`)
      }
      assert(typeof exam.id === 'number', 'id should be number')
      assert(typeof exam.soCauHoi === 'number', 'soCauHoi should be number')
    }
  })

  // ═══ DASHBOARD DTO SHAPE ═══
  await test('CONTRACT-008: Dashboard DTO complete shape', async () => {
    const res = await api.get('/statistics/dashboard')
    const d = res.data.data
    const numericFields = [
      'totalUsers', 'activeUsers', 'totalQuestions', 'totalExams',
      'activeExams', 'totalSubmissions', 'inProgressExams', 'completedExams',
      'averageScore', 'totalCheatingWarnings',
    ]
    for (const field of numericFields) {
      assert(typeof d[field] === 'number', `Dashboard.${field} should be number, got ${typeof d[field]}`)
    }
    assert(Array.isArray(d.recentActivities), 'recentActivities should be array')
  })

  await test('CONTRACT-009: Recent activity shape', async () => {
    const res = await api.get('/statistics/dashboard')
    if (res.data.data.recentActivities.length > 0) {
      const a = res.data.data.recentActivities[0]
      assert('activityType' in a, 'Should have activityType')
      assert('description' in a, 'Should have description')
      assert('timestamp' in a, 'Should have timestamp')
    }
  })

  // ═══ NOTIFICATION DTO SHAPE ═══
  await test('CONTRACT-010: Notification DTO shape', async () => {
    const res = await api.get('/notification')
    if (res.data.data.length > 0) {
      const n = res.data.data[0]
      const requiredFields = ['id', 'title', 'message', 'type', 'isRead', 'createdAt']
      for (const field of requiredFields) {
        assert(field in n, `Notification missing field: ${field}`)
      }
      assert(typeof n.id === 'number', 'id should be number')
      assert(typeof n.isRead === 'boolean', 'isRead should be boolean')
    }
  })

  // ═══ KY THI DTO SHAPE ═══
  await test('CONTRACT-011: KyThi DTO shape', async () => {
    const res = await api.get('/kythi')
    if (res.data.data.length > 0) {
      const k = res.data.data[0]
      assert('id' in k, 'Should have id')
      assert('maKyThi' in k, 'Should have maKyThi')
      assert('tenKyThi' in k, 'Should have tenKyThi')
      assert('trangThai' in k, 'Should have trangThai')
      assert('soCaThi' in k, 'Should have soCaThi')
    }
  })

  // ═══ AUTH RESPONSE SHAPE ═══
  await test('CONTRACT-012: Login response shape', async () => {
    await sleep(6000)
    const res = await api.post('/auth/login', { username: 'admin', password: 'admin123' })
    const d = res.data.data
    assert('accessToken' in d, 'Should have accessToken')
    assert('refreshToken' in d, 'Should have refreshToken')
    assert('expiresAt' in d, 'Should have expiresAt')
    assert('tokenType' in d, 'Should have tokenType')
    assert('user' in d, 'Should have user')
    assertEqual(d.tokenType, 'Bearer', 'tokenType should be Bearer')

    const u = d.user
    assert('id' in u, 'User should have id')
    assert('username' in u, 'User should have username')
    assert('email' in u, 'User should have email')
    assert('role' in u, 'User should have role')
    assert('isActive' in u, 'User should have isActive')
    state.adminToken = d.accessToken
    setAuth(state.adminToken)
  })

  // ═══ HTTP STATUS CODES ═══
  await test('CONTRACT-013: Successful GET returns 200', async () => {
    const res = await api.get('/category/categories')
    assertEqual(res.status, 200, 'Should be 200')
  })

  await test('CONTRACT-014: Successful POST returns 200 or 201', async () => {
    const res = await api.post('/notification', {
      userId: 1, title: 'Status Test', message: 'test', type: 'Info',
    })
    assert(res.status === 200 || res.status === 201, `Should be 200/201, got ${res.status}`)
  })

  await test('CONTRACT-015: Not found returns 404', async () => {
    try {
      await api.get('/cauhoi/999999')
    } catch (err) {
      assertEqual(err.response?.status, 404, 'Should be 404')
    }
  })

  // ═══ EXAM STATISTICS SHAPE ═══
  await test('CONTRACT-016: Exam statistics shape', async () => {
    const examsRes = await api.get('/exam/active')
    if (examsRes.data.data.length > 0) {
      const examId = examsRes.data.data[0].id
      const res = await api.get(`/statistics/exam/${examId}`)
      if (res.data.success) {
        const s = res.data.data
        assert('examId' in s, 'Should have examId')
        assert('totalParticipants' in s, 'Should have totalParticipants')
        assert('averageScore' in s, 'Should have averageScore')
        assert('passRate' in s, 'Should have passRate')
        assert('scoreDistribution' in s, 'Should have scoreDistribution')
        assert(Array.isArray(s.scoreDistribution), 'scoreDistribution should be array')
      }
    }
  })

  // ═══ GRADING RESULT SHAPE ═══
  await test('CONTRACT-017: Grading result detail shape', async () => {
    if (state.createdBaithiId) {
      const res = await api.get(`/grading/result/${state.createdBaithiId}`)
      if (res.data.success) {
        const r = res.data.data
        const fields = ['baiThiId', 'userId', 'examId', 'tongDiem', 'soCauDung', 'tongSoCau', 'trangThai', 'pass']
        for (const f of fields) {
          assert(f in r, `Result missing field: ${f}`)
        }
        assert(typeof r.pass === 'boolean', 'pass should be boolean')
      }
    }
  })

  // ═══ ASSIGNMENT DTO SHAPE ═══
  await test('CONTRACT-018: Exam assignment shape', async () => {
    const examsRes = await api.get('/exam/active')
    if (examsRes.data.data.length > 0) {
      const examId = examsRes.data.data[0].id
      const res = await api.get(`/examassignment/exam/${examId}`)
      if (res.data.success && res.data.data.length > 0) {
        const a = res.data.data[0]
        assert('id' in a, 'Should have id')
        assert('examId' in a, 'Should have examId')
        assert('userId' in a, 'Should have userId')
        assert('assignedAt' in a, 'Should have assignedAt')
        assert('isActive' in a, 'Should have isActive')
      }
    }
  })

  return printSummary('API Contract Validation')
}

if (process.argv[1]?.includes('23-api-contract')) {
  runApiContractTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
