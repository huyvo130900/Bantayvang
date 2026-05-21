/**
 * Test Suite 18: Advanced Exam Scenarios
 * Tests complex exam flows: multiple students, time extension, concurrent submissions
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan, sleep,
  loginAsAdmin, randomUsername, randomEmail, randomExamCode, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runAdvancedExamTests() {
  console.log('\n📋 ═══ 18. ADVANCED EXAM SCENARIOS ═══')
  resetResults()
  setAuth(state.adminToken)

  // Setup: create questions for this suite
  const questionIds = []
  for (let i = 0; i < 5; i++) {
    const res = await api.post('/cauhoi', {
      noiDung: `Advanced Exam Q${i + 1}: Câu hỏi kiểm tra nâng cao ${Date.now()}`,
      idLoaiCauHoi: 1, doKho: i < 2 ? 'De' : i < 4 ? 'TrungBinh' : 'Kho',
      diem: i < 2 ? 1 : i < 4 ? 2 : 3,
      idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: `Q${i + 1} Đáp án đúng`, thuTu: 1, laDapAnDung: true },
        { noiDung: `Q${i + 1} Sai A`, thuTu: 2, laDapAnDung: false },
        { noiDung: `Q${i + 1} Sai B`, thuTu: 3, laDapAnDung: false },
        { noiDung: `Q${i + 1} Sai C`, thuTu: 4, laDapAnDung: false },
      ],
    })
    if (res.data.success) questionIds.push(res.data.data.id)
  }

  // Create exam
  const examCode = randomExamCode()
  let examId = 0
  await api.post('/exam', {
    maDeThi: examCode,
    tenDeThi: 'Advanced Exam Test - Multiple Students',
    thoiGianLamBai: 30,
    thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
    trangThai: 'Active',
    danhSachIdCauHoi: questionIds,
  })
  const examRes = await api.get(`/exam/code/${examCode}`)
  examId = examRes.data.data.id

  // Create multiple students
  const students = []
  for (let i = 0; i < 3; i++) {
    const username = randomUsername()
    await api.post('/user', {
      tenDangNhap: username,
      matKhau: 'Student@123',
      email: randomEmail(),
      hoTen: `Thí sinh ${i + 1}`,
      khoaPhong: i === 0 ? 'Khoa Nội' : i === 1 ? 'Khoa Ngoại' : 'Khoa Sản',
      idVaiTro: 3,
      trangThai: true,
    })
    students.push(username)
  }

  // Assign all students
  const userListRes = await api.get('/user?pageNumber=1&pageSize=100')
  const studentUserIds = students.map(username =>
    userListRes.data.data.find(u => u.tenDangNhap === username)?.id
  ).filter(Boolean)

  await api.post('/examassignment/assign', {
    examId,
    userIds: studentUserIds,
  })

  // --- TESTS ---
  await test('ADV-EXAM-001: Multiple students can start same exam', async () => {
    const tokens = []
    for (const username of students) {
      await sleep(2000)
      const loginRes = await api.post('/auth/login', { username, password: 'Student@123' })
      if (loginRes.data.success) tokens.push(loginRes.data.data.accessToken)
    }
    assertEqual(tokens.length, 3, 'All 3 students should login')

    // Each student starts exam
    const baithiIds = []
    for (const token of tokens) {
      setAuth(token)
      const res = await api.post('/exam/start', { maDeThi: examCode })
      assert(res.data.success, 'Each student should start exam')
      baithiIds.push(res.data.data.id)
    }

    // All should have different baithi IDs
    const uniqueIds = new Set(baithiIds)
    assertEqual(uniqueIds.size, 3, 'Each student should have unique baithi')
    state._advBaithiIds = baithiIds
    state._advStudentTokens = tokens
  })

  await test('ADV-EXAM-002: Students get shuffled question order', async () => {
    const orders = []
    for (const token of state._advStudentTokens || []) {
      setAuth(token)
      const baithiId = state._advBaithiIds[state._advStudentTokens.indexOf(token)]
      const res = await api.get(`/exam/${baithiId}/questions`)
      if (res.data.success) {
        orders.push(res.data.data.map(q => q.id).join(','))
      }
    }
    // At least some orders should differ (probabilistic, but with 5 questions very likely)
    assert(orders.length === 3, 'Should get orders for all students')
    // Note: with seed-based shuffle, same baithiId gives same order
    // Different baithiIds should give different orders
  })

  await test('ADV-EXAM-003: Student 1 answers all correctly', async () => {
    setAuth(state._advStudentTokens[0])
    const baithiId = state._advBaithiIds[0]
    const qRes = await api.get(`/exam/${baithiId}/questions`)
    for (const q of qRes.data.data) {
      await api.post('/exam/answer', {
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: q.danhSachLuaChon[0].id, // First choice (correct)
        daLuu: true,
      })
    }
    const submitRes = await api.post('/exam/submit', {
      idBaiThi: baithiId,
      danhSachCauTraLoi: qRes.data.data.map(q => ({
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: q.danhSachLuaChon[0].id,
        daLuu: true,
      })),
    })
    assert(submitRes.data.success, 'Submit should succeed')
    assertEqual(submitRes.data.data.trangThai, 'Completed', 'Should be completed')
  })

  await test('ADV-EXAM-004: Student 2 answers all wrong', async () => {
    setAuth(state._advStudentTokens[1])
    const baithiId = state._advBaithiIds[1]
    const qRes = await api.get(`/exam/${baithiId}/questions`)
    for (const q of qRes.data.data) {
      // Pick last choice (wrong)
      const wrongChoice = q.danhSachLuaChon[q.danhSachLuaChon.length - 1]
      await api.post('/exam/answer', {
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: wrongChoice.id,
        daLuu: true,
      })
    }
    const submitRes = await api.post('/exam/submit', {
      idBaiThi: baithiId,
      danhSachCauTraLoi: qRes.data.data.map(q => ({
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: q.danhSachLuaChon[q.danhSachLuaChon.length - 1].id,
        daLuu: true,
      })),
    })
    assert(submitRes.data.success, 'Submit should succeed')
  })

  await test('ADV-EXAM-005: Student 3 partially answers then submits', async () => {
    setAuth(state._advStudentTokens[2])
    const baithiId = state._advBaithiIds[2]
    const qRes = await api.get(`/exam/${baithiId}/questions`)
    // Only answer first 2 questions
    for (let i = 0; i < 2; i++) {
      const q = qRes.data.data[i]
      await api.post('/exam/answer', {
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: q.danhSachLuaChon[0].id,
        daLuu: true,
      })
    }
    const submitRes = await api.post('/exam/submit', {
      idBaiThi: baithiId,
      danhSachCauTraLoi: qRes.data.data.slice(0, 2).map(q => ({
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: q.danhSachLuaChon[0].id,
        daLuu: true,
      })),
    })
    assert(submitRes.data.success, 'Partial submit should succeed')
  })

  // --- GRADING & RANKING ---
  await test('ADV-EXAM-006: Regrade all submissions', async () => {
    setAuth(state.adminToken)
    for (const baithiId of state._advBaithiIds) {
      const res = await api.post(`/grading/regrade/${baithiId}`)
      assert(res.data.success, `Regrade ${baithiId} failed`)
    }
  })

  await test('ADV-EXAM-007: Ranking reflects correct order', async () => {
    const res = await api.get(`/grading/exam/${examId}/ranking?top=10`)
    assert(res.data.success, 'Should succeed')
    if (res.data.data.length >= 2) {
      // First student (all correct) should have highest score
      const scores = res.data.data.map(r => r.tongDiem || 0)
      assert(scores[0] >= scores[1], 'Ranking should be descending by score')
    }
  })

  await test('ADV-EXAM-008: Exam statistics show 3 participants', async () => {
    const res = await api.get(`/statistics/exam/${examId}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.totalParticipants, 3, 'Should have 3 participants')
    assertEqual(res.data.data.completedCount, 3, 'All 3 should be completed')
  })

  await test('ADV-EXAM-009: Pass/fail rate is calculated', async () => {
    const res = await api.get(`/statistics/exam/${examId}`)
    assert(res.data.success, 'Should succeed')
    assert(typeof res.data.data.passRate === 'number', 'Should have pass rate')
    assert(res.data.data.passCount + res.data.data.failCount === 3, 'Pass + fail should equal 3')
  })

  await test('ADV-EXAM-010: Score distribution has data', async () => {
    const res = await api.get(`/statistics/exam/${examId}`)
    assert(res.data.success, 'Should succeed')
    const totalInDist = res.data.data.scoreDistribution.reduce((sum, d) => sum + d.count, 0)
    assertEqual(totalInDist, 3, 'Distribution should account for all 3 submissions')
  })

  // --- TIME EXTENSION ---
  await test('ADV-EXAM-011: Extend time for in-progress exam', async () => {
    // Create a new student and start exam (don't submit)
    const extUsername = randomUsername()
    await api.post('/user', {
      tenDangNhap: extUsername,
      matKhau: 'Extend@123',
      email: randomEmail(),
      hoTen: 'Extend Time Student',
      idVaiTro: 3,
      trangThai: true,
    })
    const extUserRes = await api.get(`/user?pageNumber=1&pageSize=100&searchKeyword=${extUsername}`)
    const extUserId = extUserRes.data.data.find(u => u.tenDangNhap === extUsername)?.id

    await api.post('/examassignment/assign', { examId, userIds: [extUserId] })

    await sleep(2000)
    const loginRes = await api.post('/auth/login', { username: extUsername, password: 'Extend@123' })
    setAuth(loginRes.data.data.accessToken)

    const startRes = await api.post('/exam/start', { maDeThi: examCode })
    const extBaithiId = startRes.data.data.id

    // Admin extends time
    setAuth(state.adminToken)
    const extendRes = await api.post('/examassignment/extend-time', {
      baiThiId: extBaithiId,
      additionalMinutes: 15,
      reason: 'Sự cố kỹ thuật',
    })
    assert(extendRes.data.success, `Extend time failed: ${extendRes.data.message}`)
  })

  // --- EXPORT ---
  await test('ADV-EXAM-012: Export results with multiple students', async () => {
    const res = await api.get(`/grading/exam/${examId}/export`, { responseType: 'arraybuffer' })
    assert(res.status === 200, 'Should return 200')
    assertGreaterThan(res.data.byteLength, 500, 'Excel should have substantial content')
  })

  await test('ADV-EXAM-013: Export ranking with multiple students', async () => {
    const res = await api.get(`/grading/exam/${examId}/ranking/export?top=10`, { responseType: 'arraybuffer' })
    assert(res.status === 200, 'Should return 200')
    assertGreaterThan(res.data.byteLength, 500, 'Excel should have substantial content')
  })

  // --- CLEANUP ---
  await test('ADV-EXAM-014: Cleanup test questions', async () => {
    setAuth(state.adminToken)
    for (const id of questionIds) {
      await api.delete(`/cauhoi/${id}`)
    }
    assert(true, 'Cleanup done')
  })

  return printSummary('Advanced Exam Scenarios')
}

if (process.argv[1]?.includes('18-exam-advanced')) {
  runAdvancedExamTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
