/**
 * BanTayVang API Integration Test Suite
 * Tests full flow: Auth → Users → Questions → Exams → Taking → Grading → KyThi → Notifications → Statistics → AuditLog
 *
 * Usage: node tests/api-test.mjs
 * Requires: BE running at https://localhost:7249
 */

import https from 'https'
import axios from 'axios'

// Skip SSL verification for localhost
const api = axios.create({
  baseURL: 'https://localhost:7249/api',
  headers: { 'Content-Type': 'application/json' },
  httpsAgent: new https.Agent({ rejectUnauthorized: false }),
  timeout: 15000,
})

// Test state
let adminToken = ''
let studentToken = ''
let createdUserId = 0
let createdQuestionId = 0
let createdExamId = ''
let createdBaithiId = 0
let createdKyThiId = 0
let createdCaThiId = 0
let createdNotificationId = 0

// Helpers
let passed = 0
let failed = 0
const results = []
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms))

function setAuth(token) {
  api.defaults.headers.common['Authorization'] = `Bearer ${token}`
}

async function test(name, fn) {
  try {
    await fn()
    passed++
    results.push({ name, status: '✅ PASS' })
    console.log(`  ✅ ${name}`)
  } catch (error) {
    failed++
    const msg = error.response?.data?.message || error.message || String(error)
    results.push({ name, status: '❌ FAIL', error: msg })
    console.log(`  ❌ ${name} → ${msg}`)
  }
}

function assert(condition, message) {
  if (!condition) throw new Error(message || 'Assertion failed')
}

// ═══════════════════════════════════════════════════════════
// TEST SUITES
// ═══════════════════════════════════════════════════════════

async function testAuth() {
  console.log('\n🔐 === AUTH TESTS ===')

  await test('Login with admin/admin123', async () => {
    const res = await api.post('/auth/login', { username: 'admin', password: 'admin123' })
    assert(res.data.success, 'Login should succeed')
    assert(res.data.data.accessToken, 'Should return access token')
    assert(res.data.data.refreshToken, 'Should return refresh token')
    assert(res.data.data.user.role === 'Admin', 'Should be Admin role')
    adminToken = res.data.data.accessToken
    setAuth(adminToken)
  })

  await test('Login with wrong password should fail', async () => {
    try {
      const res = await api.post('/auth/login', { username: 'admin', password: 'wrongpass' })
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      // 400/401 response is also acceptable
      assert(err.response?.status >= 400, 'Should return error status')
    }
  })

  await test('Get current user (GET /auth/me)', async () => {
    const res = await api.get('/auth/me')
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.username === 'admin', 'Should be admin')
  })

  await test('Validate token (GET /auth/validate)', async () => {
    await sleep(1000) // Avoid rate limit
    const res = await api.get('/auth/validate')
    assert(res.data.success, 'Token should be valid')
  })

  await test('Refresh token', async () => {
    await sleep(1000) // Avoid rate limit
    // First login to get refresh token
    const loginRes = await api.post('/auth/login', { username: 'admin', password: 'admin123' })
    const refreshToken = loginRes.data.data.refreshToken
    const res = await api.post('/auth/refresh', { refreshToken })
    assert(res.data.success, 'Refresh should succeed')
    assert(res.data.data.accessToken, 'Should return new access token')
    adminToken = res.data.data.accessToken
    setAuth(adminToken)
  })
}

async function testUsers() {
  console.log('\n👥 === USER MANAGEMENT TESTS ===')

  await test('Get all users', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=10')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('Create student user', async () => {
    const timestamp = Date.now()
    const res = await api.post('/user', {
      tenDangNhap: `teststudent_${timestamp}`,
      matKhau: 'Test@123456',
      email: `test_${timestamp}@test.com`,
      hoTen: 'Thí Sinh Test',
      maNhanVien: `NV_${timestamp}`,
      chucDanh: 'Điều dưỡng',
      khoaPhong: 'Khoa Nội',
      idVaiTro: 3,
      trangThai: true,
    })
    assert(res.data.success, `Create user failed: ${res.data.message}`)
    assert(res.data.data.id > 0, 'Should return user ID')
    createdUserId = res.data.data.id
  })

  await test('Get user by ID', async () => {
    const res = await api.get(`/user/${createdUserId}`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.hoTen === 'Thí Sinh Test', 'Name should match')
  })

  await test('Update user', async () => {
    const res = await api.put(`/user/${createdUserId}`, {
      email: `updated_${Date.now()}@test.com`,
      hoTen: 'Thí Sinh Updated',
      maNhanVien: 'NV_UPDATED',
      chucDanh: 'Bác sĩ',
      khoaPhong: 'Khoa Ngoại',
      idVaiTro: 3,
      trangThai: true,
    })
    assert(res.data.success, `Update failed: ${res.data.message}`)
  })

  await test('Deactivate user', async () => {
    const res = await api.post(`/user/${createdUserId}/deactivate`)
    assert(res.data.success, 'Deactivate should succeed')
  })

  await test('Activate user', async () => {
    const res = await api.post(`/user/${createdUserId}/activate`)
    assert(res.data.success, 'Activate should succeed')
  })

  await test('Reset user password', async () => {
    const res = await api.post(`/user/${createdUserId}/reset-password`, { newPassword: 'NewPass@123' })
    assert(res.data.success, 'Reset password should succeed')
  })

  await test('Login as created student', async () => {
    await sleep(2000) // Avoid rate limit on auth endpoints
    // Get username first
    const userRes = await api.get(`/user/${createdUserId}`)
    const username = userRes.data.data.tenDangNhap
    const res = await api.post('/auth/login', { username, password: 'NewPass@123' })
    assert(res.data.success, `Student login failed: ${res.data.message}`)
    studentToken = res.data.data.accessToken
  })

  await test('Filter users by role', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=10&idVaiTro=3')
    assert(res.data.success, 'Should succeed')
  })
}

async function testCategories() {
  console.log('\n📂 === CATEGORY TESTS ===')
  setAuth(adminToken)

  await test('Get all categories', async () => {
    const res = await api.get('/category/categories')
    assert(res.data.success, 'Should succeed')
  })

  await test('Create category', async () => {
    const res = await api.post('/category/categories', {
      tenDanhMuc: `Test Category ${Date.now()}`,
      mota: 'Danh mục test automation',
    })
    assert(res.data.success, `Create category failed: ${res.data.message}`)
  })

  await test('Get all question types', async () => {
    const res = await api.get('/category/types')
    assert(res.data.success, 'Should succeed')
  })

  await test('Create question type', async () => {
    const res = await api.post('/category/types', {
      tenLoai: `Test Type ${Date.now()}`,
      moTa: 'Loại câu hỏi test',
    })
    assert(res.data.success, `Create type failed: ${res.data.message}`)
  })
}

async function testQuestions() {
  console.log('\n❓ === QUESTION TESTS ===')
  setAuth(adminToken)

  await test('Create question with choices', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: 'Câu hỏi test automation: Vitamin C có tác dụng gì?',
      idLoaiCauHoi: 1,
      doKho: 'De',
      diem: 1,
      idDanhMuc: 1,
      khoaPhong: 'Khoa Nội',
      danhSachLuaChon: [
        { noiDung: 'Tăng sức đề kháng', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Giảm cân', thuTu: 2, laDapAnDung: false },
        { noiDung: 'Tăng huyết áp', thuTu: 3, laDapAnDung: false },
        { noiDung: 'Gây mất ngủ', thuTu: 4, laDapAnDung: false },
      ],
    })
    assert(res.data.success, `Create question failed: ${res.data.message}`)
    createdQuestionId = res.data.data.id
  })

  await test('Get question by ID', async () => {
    const res = await api.get(`/cauhoi/${createdQuestionId}`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.danhSachLuaChon.length === 4, 'Should have 4 choices')
  })

  await test('Update question', async () => {
    const res = await api.put(`/cauhoi/${createdQuestionId}`, {
      id: createdQuestionId,
      noiDung: 'Câu hỏi đã cập nhật: Vitamin C có tác dụng chính là gì?',
      idLoaiCauHoi: 1,
      doKho: 'TrungBinh',
      diem: 2,
      idDanhMuc: 1,
      khoaPhong: 'Khoa Nội',
      danhSachLuaChon: [
        { noiDung: 'Tăng sức đề kháng', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Giảm cân', thuTu: 2, laDapAnDung: false },
        { noiDung: 'Tăng huyết áp', thuTu: 3, laDapAnDung: false },
        { noiDung: 'Không có tác dụng', thuTu: 4, laDapAnDung: false },
      ],
    })
    assert(res.data.success, `Update question failed: ${res.data.message}`)
  })

  await test('Get filtered questions', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=10&doKho=TrungBinh')
    assert(res.data.success, 'Should succeed')
  })

  await test('Get random questions', async () => {
    const res = await api.get('/cauhoi/random?count=5')
    assert(res.data.success, 'Should succeed')
  })
}

async function testExams() {
  console.log('\n📝 === EXAM MANAGEMENT TESTS ===')
  setAuth(adminToken)

  const examCode = `TEST_${Date.now()}`

  await test('Create exam', async () => {
    const res = await api.post('/exam', {
      maDeThi: examCode,
      tenDeThi: 'Đề thi test automation',
      thoiGianLamBai: 30,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(), // started 1 min ago
      trangThai: 'Active',
      danhSachIdCauHoi: [createdQuestionId],
    })
    assert(res.data.success, `Create exam failed: ${res.data.message}`)
    createdExamId = examCode
  })

  await test('Get active exams', async () => {
    const res = await api.get('/exam/active')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('Get exam by code', async () => {
    const res = await api.get(`/exam/code/${createdExamId}`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.maDeThi === createdExamId, 'Code should match')
  })

  await test('Assign student to exam', async () => {
    const res = await api.post('/examassignment/assign', {
      examId: (await api.get(`/exam/code/${createdExamId}`)).data.data.id,
      userIds: [createdUserId],
      note: 'Test assignment',
    })
    assert(res.data.success, `Assign failed: ${res.data.message}`)
  })

  await test('Check assignment', async () => {
    const examId = (await api.get(`/exam/code/${createdExamId}`)).data.data.id
    const res = await api.get(`/examassignment/check/${examId}/${createdUserId}`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data === true, 'Should be assigned')
  })
}

async function testExamTaking() {
  console.log('\n🎯 === EXAM TAKING TESTS ===')
  setAuth(studentToken)

  await test('Start exam', async () => {
    const res = await api.post('/exam/start', { maDeThi: createdExamId })
    assert(res.data.success, `Start exam failed: ${res.data.message}`)
    createdBaithiId = res.data.data.id
    assert(res.data.data.trangThai === 'InProgress', 'Status should be InProgress')
  })

  await test('Get exam questions', async () => {
    const res = await api.get(`/exam/${createdBaithiId}/questions`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length > 0, 'Should have questions')
  })

  await test('Save answer', async () => {
    // Get questions to find choice ID
    const qRes = await api.get(`/exam/${createdBaithiId}/questions`)
    const question = qRes.data.data[0]
    const correctChoice = question.danhSachLuaChon[0] // first choice (correct one)

    const res = await api.post('/exam/answer', {
      idBaiThi: createdBaithiId,
      idCauHoi: question.id,
      idLuaChonDaChon: correctChoice.id,
      daLuu: true,
    })
    assert(res.data.success, `Save answer failed: ${res.data.message}`)
  })

  await test('Get exam progress', async () => {
    const res = await api.get(`/exam/${createdBaithiId}/progress`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.trangThai === 'InProgress', 'Should still be in progress')
  })

  await test('Log cheating warning', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: createdBaithiId,
      loaiCanhBao: 'TAB_SWITCH',
      moTa: 'Test automation warning',
    })
    assert(res.data.success, 'Should succeed')
  })

  await test('Get warning count', async () => {
    const res = await api.get(`/exam/${createdBaithiId}/warnings`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data >= 1, 'Should have at least 1 warning')
  })

  await test('Submit exam', async () => {
    const qRes = await api.get(`/exam/${createdBaithiId}/questions`)
    const question = qRes.data.data[0]
    const choice = question.danhSachLuaChon[0]

    const res = await api.post('/exam/submit', {
      idBaiThi: createdBaithiId,
      danhSachCauTraLoi: [{
        idBaiThi: createdBaithiId,
        idCauHoi: question.id,
        idLuaChonDaChon: choice.id,
        daLuu: true,
      }],
    })
    assert(res.data.success, `Submit failed: ${res.data.message}`)
    assert(res.data.data.trangThai === 'Completed', 'Should be Completed')
  })
}

async function testGrading() {
  console.log('\n🏆 === GRADING TESTS ===')
  setAuth(adminToken)

  await test('Get result detail', async () => {
    const res = await api.get(`/grading/result/${createdBaithiId}`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.baiThiId === createdBaithiId, 'BaiThi ID should match')
  })

  await test('Get results by exam', async () => {
    const examId = (await api.get(`/exam/code/${createdExamId}`)).data.data.id
    const res = await api.get(`/grading/exam/${examId}/results`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length > 0, 'Should have results')
  })

  await test('Regrade exam', async () => {
    const res = await api.post(`/grading/regrade/${createdBaithiId}`)
    assert(res.data.success, `Regrade failed: ${res.data.message}`)
  })

  await test('Get ranking', async () => {
    const examId = (await api.get(`/exam/code/${createdExamId}`)).data.data.id
    const res = await api.get(`/grading/exam/${examId}/ranking?top=10`)
    assert(res.data.success, 'Should succeed')
  })

  await test('Auto-grade all', async () => {
    const res = await api.post('/grading/auto-grade-all')
    assert(res.data.success, 'Should succeed')
  })
}

async function testKyThi() {
  console.log('\n📅 === KY THI TESTS ===')
  setAuth(adminToken)

  await test('Create ky thi', async () => {
    const res = await api.post('/kythi', {
      maKyThi: `KT_TEST_${Date.now()}`,
      tenKyThi: 'Kỳ thi test automation',
      moTa: 'Mô tả test',
      loaiKyThi: 'BanTayVang',
      thoiGianBatDau: new Date().toISOString(),
      thoiGianKetThuc: new Date(Date.now() + 86400000).toISOString(),
      donViToChuc: 'Phòng Điều dưỡng',
    })
    assert(res.data.success, `Create ky thi failed: ${res.data.message}`)
    createdKyThiId = res.data.data.id
  })

  await test('Get all ky thi', async () => {
    const res = await api.get('/kythi')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('Get ky thi by ID', async () => {
    const res = await api.get(`/kythi/${createdKyThiId}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('Update ky thi status', async () => {
    const res = await api.post(`/kythi/${createdKyThiId}/status`, JSON.stringify('DangDienRa'), {
      headers: { 'Content-Type': 'application/json' },
    })
    assert(res.data.success, `Update status failed: ${res.data.message}`)
  })

  await test('Create ca thi', async () => {
    const examId = (await api.get(`/exam/code/${createdExamId}`)).data.data.id
    const res = await api.post('/kythi/ca-thi', {
      kyThiId: createdKyThiId,
      deThiId: examId,
      tenCa: 'Ca 1 - Sáng',
      thoiGianBatDau: new Date().toISOString(),
      thoiGianKetThuc: new Date(Date.now() + 3600000).toISOString(),
      soLuongToiDa: 50,
      ghiChu: 'Ca thi test',
    })
    assert(res.data.success, `Create ca thi failed: ${res.data.message}`)
    createdCaThiId = res.data.data.id
  })

  await test('Get ca thi by ky thi', async () => {
    const res = await api.get(`/kythi/${createdKyThiId}/ca-thi`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length > 0, 'Should have ca thi')
  })

  await test('Delete ca thi', async () => {
    const res = await api.delete(`/kythi/ca-thi/${createdCaThiId}`)
    assert(res.data.success, `Delete ca thi failed: ${res.data.message}`)
  })
}

async function testNotifications() {
  console.log('\n🔔 === NOTIFICATION TESTS ===')
  setAuth(adminToken)

  await test('Create notification', async () => {
    const res = await api.post('/notification', {
      userId: createdUserId,
      title: 'Test Notification',
      message: 'This is a test notification from automation',
      type: 'Info',
    })
    assert(res.data.success, `Create notification failed: ${res.data.message}`)
    createdNotificationId = res.data.data.id
  })

  await test('Get notifications', async () => {
    const res = await api.get('/notification')
    assert(res.data.success, 'Should succeed')
  })

  await test('Get unread count', async () => {
    const res = await api.get('/notification/unread-count')
    assert(res.data.success, 'Should succeed')
  })

  await test('Broadcast notification', async () => {
    const res = await api.post('/notification/broadcast', {
      title: 'Broadcast Test',
      message: 'Broadcast from automation test',
      type: 'Info',
    })
    assert(res.data.success, `Broadcast failed: ${res.data.message}`)
  })

  await test('Mark as read', async () => {
    // Notification was created for admin (userId null = broadcast), so admin can read it
    // Get admin's notifications first
    const listRes = await api.get('/notification')
    const unread = listRes.data.data?.find(n => !n.isRead)
    if (unread) {
      const res = await api.post(`/notification/${unread.id}/read`)
      assert(res.data.success, `Mark read failed: ${res.data.message}`)
    } else {
      // No unread notifications, create one for admin and mark it
      const createRes = await api.post('/notification', { title: 'Read Test', message: 'test', type: 'Info' })
      const res = await api.post(`/notification/${createRes.data.data.id}/read`)
      assert(res.data.success, `Mark read failed: ${res.data.message}`)
    }
  })

  await test('Mark all as read', async () => {
    const res = await api.post('/notification/mark-all-read')
    assert(res.data.success, 'Should succeed')
  })

  await test('Get upcoming exams', async () => {
    const res = await api.get('/notification/upcoming-exams')
    assert(res.data.success, 'Should succeed')
  })

  await test('Get current exams', async () => {
    const res = await api.get('/notification/current-exams')
    assert(res.data.success, 'Should succeed')
  })
}

async function testStatistics() {
  console.log('\n📊 === STATISTICS TESTS ===')
  setAuth(adminToken)

  await test('Get dashboard', async () => {
    const res = await api.get('/statistics/dashboard')
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.totalUsers > 0, 'Should have users')
  })

  await test('Get exam statistics', async () => {
    const examId = (await api.get(`/exam/code/${createdExamId}`)).data.data.id
    const res = await api.get(`/statistics/exam/${examId}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('Get top performers', async () => {
    const res = await api.get('/statistics/top-performers?top=5')
    assert(res.data.success, 'Should succeed')
  })

  await test('Get my history (student)', async () => {
    setAuth(studentToken)
    const res = await api.get('/statistics/my-history')
    assert(res.data.success, 'Should succeed')
    setAuth(adminToken)
  })
}

async function testAuditLog() {
  console.log('\n📋 === AUDIT LOG TESTS ===')
  setAuth(adminToken)

  await test('Get recent logs', async () => {
    const res = await api.get('/auditlog/recent?top=50')
    assert(res.data.success, 'Should succeed')
  })

  await test('Get logs by user', async () => {
    const res = await api.get(`/auditlog/user/1?top=10`)
    assert(res.data.success, 'Should succeed')
  })

  await test('Get logs by exam session', async () => {
    const res = await api.get(`/auditlog/exam-session/${createdBaithiId}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('Search logs', async () => {
    const res = await api.get('/auditlog/search?actionType=POST')
    assert(res.data.success, 'Should succeed')
  })
}

async function testCleanup() {
  console.log('\n🧹 === CLEANUP ===')
  setAuth(adminToken)

  await test('Delete created question', async () => {
    const res = await api.delete(`/cauhoi/${createdQuestionId}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('Delete ky thi', async () => {
    // First change status back
    await api.post(`/kythi/${createdKyThiId}/status`, JSON.stringify('DaKetThuc'), {
      headers: { 'Content-Type': 'application/json' },
    })
    const res = await api.delete(`/kythi/${createdKyThiId}`)
    assert(res.data.success, `Delete ky thi failed: ${res.data.message}`)
  })
}

// ═══════════════════════════════════════════════════════════
// MAIN
// ═══════════════════════════════════════════════════════════

async function main() {
  console.log('╔══════════════════════════════════════════════════╗')
  console.log('║  BanTayVang API Integration Test Suite           ║')
  console.log('║  Target: https://localhost:7249                  ║')
  console.log('╚══════════════════════════════════════════════════╝')

  const startTime = Date.now()

  await testAuth()
  await testUsers()
  await testCategories()
  await testQuestions()
  await testExams()
  await testExamTaking()
  await testGrading()
  await testKyThi()
  await testNotifications()
  await testStatistics()
  await testAuditLog()
  await testCleanup()

  const duration = ((Date.now() - startTime) / 1000).toFixed(1)

  console.log('\n╔══════════════════════════════════════════════════╗')
  console.log(`║  RESULTS: ${passed} passed, ${failed} failed (${duration}s)`)
  console.log('╚══════════════════════════════════════════════════╝')

  if (failed > 0) {
    console.log('\n❌ Failed tests:')
    results.filter(r => r.status.includes('FAIL')).forEach(r => {
      console.log(`   • ${r.name}: ${r.error}`)
    })
  }

  process.exit(failed > 0 ? 1 : 0)
}

main().catch((err) => {
  console.error('Fatal error:', err.message)
  process.exit(1)
})
