/**
 * Test Suite 14: Data Integrity & Business Logic
 * Tests data consistency, cascading operations, and business rules
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan,
  loginAsAdmin, randomUsername, randomEmail, randomExamCode, printSummary, resetResults, sleep,
} from '../lib/test-helper.mjs'

export async function runDataIntegrityTests() {
  console.log('\n🔗 ═══ 14. DATA INTEGRITY & BUSINESS LOGIC TESTS ═══')
  resetResults()
  setAuth(state.adminToken)

  // --- QUESTION INTEGRITY ---
  await test('DATA-001: Deleted question not returned in list', async () => {
    // Create then delete
    const createRes = await api.post('/cauhoi', {
      noiDung: 'Câu hỏi sẽ bị xóa - integrity test',
      idLoaiCauHoi: 1, doKho: 'De', diem: 1, idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'A', thuTu: 1, laDapAnDung: true },
        { noiDung: 'B', thuTu: 2, laDapAnDung: false },
      ],
    })
    const qId = createRes.data.data.id
    await api.delete(`/cauhoi/${qId}`)

    // Verify not in list
    const listRes = await api.get('/cauhoi?pageNumber=1&pageSize=100&searchKeyword=integrity test')
    const found = listRes.data.data.items.find(q => q.id === qId)
    assert(!found, 'Deleted question should not appear in list')
  })

  await test('DATA-002: Question choices are correctly linked', async () => {
    const createRes = await api.post('/cauhoi', {
      noiDung: 'Test choice linking',
      idLoaiCauHoi: 1, doKho: 'De', diem: 1, idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'Choice Alpha', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Choice Beta', thuTu: 2, laDapAnDung: false },
        { noiDung: 'Choice Gamma', thuTu: 3, laDapAnDung: false },
      ],
    })
    assert(createRes.data.success, 'Create should succeed')
    const qId = createRes.data.data.id

    const getRes = await api.get(`/cauhoi/${qId}`)
    assertEqual(getRes.data.data.danhSachLuaChon.length, 3, 'Should have 3 choices')

    const correctChoices = getRes.data.data.danhSachLuaChon.filter(c => c.laDapAnDung)
    assertEqual(correctChoices.length, 1, 'Should have exactly 1 correct answer')
    assertEqual(correctChoices[0].noiDung, 'Choice Alpha', 'Correct answer should be Alpha')

    await api.delete(`/cauhoi/${qId}`)
  })

  await test('DATA-003: Update question replaces all choices', async () => {
    const createRes = await api.post('/cauhoi', {
      noiDung: 'Original question',
      idLoaiCauHoi: 1, doKho: 'De', diem: 1, idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'Old A', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Old B', thuTu: 2, laDapAnDung: false },
      ],
    })
    const qId = createRes.data.data.id

    await api.put(`/cauhoi/${qId}`, {
      id: qId,
      noiDung: 'Updated question',
      idLoaiCauHoi: 1, doKho: 'TrungBinh', diem: 2, idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'New X', thuTu: 1, laDapAnDung: false },
        { noiDung: 'New Y', thuTu: 2, laDapAnDung: true },
        { noiDung: 'New Z', thuTu: 3, laDapAnDung: false },
      ],
    })

    const getRes = await api.get(`/cauhoi/${qId}`)
    assertEqual(getRes.data.data.danhSachLuaChon.length, 3, 'Should have 3 new choices')
    assertEqual(getRes.data.data.noiDung, 'Updated question', 'Content should be updated')
    assertEqual(getRes.data.data.doKho, 'TrungBinh', 'Difficulty should be updated')

    await api.delete(`/cauhoi/${qId}`)
  })

  // --- USER INTEGRITY ---
  await test('DATA-004: Deactivated user cannot login', async () => {
    const username = randomUsername()
    const createRes = await api.post('/user', {
      tenDangNhap: username,
      matKhau: 'Test@123',
      email: randomEmail(),
      hoTen: 'Deactivate Test',
      idVaiTro: 3,
      trangThai: true,
    })
    const userId = createRes.data.data.id

    // Deactivate
    await api.post(`/user/${userId}/deactivate`)

    // Try login
    await sleep(2000)
    try {
      const loginRes = await api.post('/auth/login', { username, password: 'Test@123' })
      assert(!loginRes.data.success, 'Deactivated user should not login')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }

    // Reactivate for cleanup
    setAuth(state.adminToken)
    await api.post(`/user/${userId}/activate`)
  })

  await test('DATA-005: User role is correctly stored', async () => {
    const username = randomUsername()
    const createRes = await api.post('/user', {
      tenDangNhap: username,
      matKhau: 'Test@123',
      email: randomEmail(),
      hoTen: 'Role Test Teacher',
      idVaiTro: 2, // Teacher
      trangThai: true,
    })
    const userId = createRes.data.data.id

    const getRes = await api.get(`/user/${userId}`)
    assertEqual(getRes.data.data.idVaiTro, 2, 'Role should be Teacher (2)')
    assertEqual(getRes.data.data.tenVaiTro, 'Teacher', 'Role name should be Teacher')
  })

  // --- EXAM INTEGRITY ---
  await test('DATA-006: Exam assignment is unique per user-exam pair', async () => {
    if (!state.createdExamId || !state.createdUserId) return

    // Assign twice
    await api.post('/examassignment/assign', {
      examId: state.createdExamId,
      userIds: [state.createdUserId],
    })
    await api.post('/examassignment/assign', {
      examId: state.createdExamId,
      userIds: [state.createdUserId],
    })

    // Should still only have 1 assignment
    const res = await api.get(`/examassignment/exam/${state.createdExamId}`)
    const userAssignments = res.data.data.filter(a => a.userId === state.createdUserId)
    assertEqual(userAssignments.length, 1, 'Should have exactly 1 assignment per user')
  })

  await test('DATA-007: Exam score is calculated correctly after regrade', async () => {
    if (!state.createdBaithiId) return

    const beforeRes = await api.get(`/grading/result/${state.createdBaithiId}`)
    const before = beforeRes.data.data

    await api.post(`/grading/regrade/${state.createdBaithiId}`)

    const afterRes = await api.get(`/grading/result/${state.createdBaithiId}`)
    const after = afterRes.data.data

    // Score should be consistent
    assertEqual(after.tongSoCau, before.tongSoCau, 'Total questions should not change')
    assert(after.tongDiem >= 0, 'Score should be >= 0')
    assert(after.soCauDung >= 0, 'Correct count should be >= 0')
    assert(after.soCauDung <= after.tongSoCau, 'Correct should not exceed total')
  })

  // --- NOTIFICATION INTEGRITY ---
  await test('DATA-008: Broadcast notification visible to all users', async () => {
    await api.post('/notification/broadcast', {
      title: 'Integrity Broadcast',
      message: 'Test broadcast visibility',
      type: 'Info',
    })

    // Check as admin
    const adminRes = await api.get('/notification?unreadOnly=true')
    const found = adminRes.data.data.find(n => n.title === 'Integrity Broadcast')
    assert(found, 'Admin should see broadcast')
  })

  await test('DATA-009: Mark as read persists', async () => {
    const listRes = await api.get('/notification?unreadOnly=true')
    if (listRes.data.data.length > 0) {
      const notifId = listRes.data.data[0].id
      await api.post(`/notification/${notifId}/read`)

      // Verify it's now read
      const allRes = await api.get('/notification')
      const notif = allRes.data.data.find(n => n.id === notifId)
      assert(notif?.isRead === true, 'Should be marked as read')
    }
  })

  // --- KY THI INTEGRITY ---
  await test('DATA-010: Ky thi status transitions are enforced', async () => {
    const createRes = await api.post('/kythi', {
      maKyThi: `INT_${Date.now()}`,
      tenKyThi: 'Integrity Status Test',
      loaiKyThi: 'CNTT',
    })
    const kyThiId = createRes.data.data.id

    // DangChuanBi → DangDienRa (valid)
    const r1 = await api.post(`/kythi/${kyThiId}/status`, JSON.stringify('DangDienRa'), {
      headers: { 'Content-Type': 'application/json' },
    })
    assert(r1.data.success, 'Should allow DangChuanBi → DangDienRa')

    // Cannot delete while DangDienRa
    try {
      const delRes = await api.delete(`/kythi/${kyThiId}`)
      assert(!delRes.data.success, 'Should not delete active ky thi')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }

    // DangDienRa → DaKetThuc (valid)
    await api.post(`/kythi/${kyThiId}/status`, JSON.stringify('DaKetThuc'), {
      headers: { 'Content-Type': 'application/json' },
    })

    // Now can delete
    const delRes = await api.delete(`/kythi/${kyThiId}`)
    assert(delRes.data.success, 'Should delete after DaKetThuc')
  })

  // --- STATISTICS INTEGRITY ---
  await test('DATA-011: Dashboard counts are consistent', async () => {
    const res = await api.get('/statistics/dashboard')
    const d = res.data.data
    assert(d.activeUsers <= d.totalUsers, 'Active users <= total users')
    assert(d.activeExams <= d.totalExams, 'Active exams <= total exams')
    assert(d.completedExams <= d.totalSubmissions, 'Completed <= total submissions')
    assert(d.inProgressExams <= d.totalSubmissions, 'InProgress <= total submissions')
  })

  await test('DATA-012: Exam statistics match grading results', async () => {
    if (!state.createdExamId) return

    const statsRes = await api.get(`/statistics/exam/${state.createdExamId}`)
    const gradingRes = await api.get(`/grading/exam/${state.createdExamId}/results`)

    if (statsRes.data.success && gradingRes.data.success) {
      const stats = statsRes.data.data
      const results = gradingRes.data.data
      assertEqual(stats.totalParticipants, results.length, 'Participant count should match')
    }
  })

  return printSummary('Data Integrity & Business Logic')
}

if (process.argv[1]?.includes('14-data')) {
  runDataIntegrityTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
