/**
 * Test Suite 25: Candidate Retake Exams Selection Rules
 */
import {
  api, state, setAuth, test, assert, assertEqual,
  loginAsAdmin, loginAsStudent, printSummary, resetResults, toLocalISOString,
  randomUsername, randomEmail, sleep,
} from '../lib/test-helper.mjs'

export async function runRetakeTests() {
  console.log('\n📝 ═══ 25. CANDIDATE RETAKE EXAMS TESTS ═══')
  resetResults()
  await loginAsAdmin()

  // 1. Create a question
  let questionId = 0
  await test('RETAKE-001: Create question for tests', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: `Thời gian tối thiểu sát khuẩn tay? ${Date.now()}_${Math.random().toString().slice(-4)}`,
      idLoaiCauHoi: 1,
      doKho: 'De',
      diem: 1,
      idDanhMuc: 1,
      khoaPhong: 'Khoa KSNK',
      danhSachLuaChon: [
        { noiDung: '10 giây', thuTu: 1, laDapAnDung: false },
        { noiDung: '20 giây', thuTu: 2, laDapAnDung: true },
      ],
    })
    assert(res.data.success, 'Should create question')
    questionId = res.data.data.id
    console.log("  [DEBUG] Created question ID:", questionId)
  })

  // 2. Create a KyThi
  let kyThiId = 0
  const maKyThi = `KT_RETAKE_${Date.now()}`
  await test('RETAKE-002: Create ky thi', async () => {
    const res = await api.post('/kythi', {
      maKyThi,
      tenKyThi: 'Kỳ thi Thử nghiệm Thi Lại',
      moTa: 'Automation test cho tính năng thi lại nhiều đề',
      khoaPhongId: 1,
      thoiGianBatDau: toLocalISOString(new Date(Date.now() + 2000)), // 2 seconds in the future
      thoiGianKetThuc: toLocalISOString(new Date(Date.now() + 3600000)), // 1 hour future
      donViToChuc: 'Phòng KSNK',
    })
    assert(res.data.success, 'Should create KyThi')
    kyThiId = res.data.data.id
    console.log("  [DEBUG] Created KyThi ID:", kyThiId)

    // Start the KyThi
    await api.post(`/kythi/${kyThiId}/status`, JSON.stringify('DangDienRa'), {
      headers: { 'Content-Type': 'application/json' },
    })

    // Wait for the KyThi to become active
    await sleep(3000)
  })

  // 3. Create 3 different exams (Exam A, B, C) under this KyThi
  let examAId = 0, examBId = 0, examCId = 0
  const codeA = `EXAM_A_${Date.now()}`
  const codeB = `EXAM_B_${Date.now()}`
  const codeC = `EXAM_C_${Date.now()}`

  await test('RETAKE-003: Create Exam A, B, C under KyThi', async () => {
    const resA = await api.post('/exam', {
      maDeThi: codeA,
      tenDeThi: 'Đề thi A',
      thoiGianLamBai: 30,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: [questionId],
      kyThiId: kyThiId,
    })
    assert(resA.data.success, `Create Exam A failed: ${resA.data.message}`)
    examAId = resA.data.data.id
    console.log("  [DEBUG] Created Exam A ID:", examAId)

    const resB = await api.post('/exam', {
      maDeThi: codeB,
      tenDeThi: 'Đề thi B',
      thoiGianLamBai: 30,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: [questionId],
      kyThiId: kyThiId,
    })
    assert(resB.data.success, `Create Exam B failed: ${resB.data.message}`)
    examBId = resB.data.data.id
    console.log("  [DEBUG] Created Exam B ID:", examBId)

    const resC = await api.post('/exam', {
      maDeThi: codeC,
      tenDeThi: 'Đề thi C',
      thoiGianLamBai: 30,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: [questionId],
      kyThiId: kyThiId,
    })
    assert(resC.data.success, `Create Exam C failed: ${resC.data.message}`)
    examCId = resC.data.data.id
    console.log("  [DEBUG] Created Exam C ID:", examCId)
  })

  // 4. Create a student user via Admin User API to bypass rate limit
  const studentUsername = randomUsername()
  const studentPassword = 'Student@123'
  let studentUserId = 0

  await test('RETAKE-004: Create student user', async () => {
    const res = await api.post('/user', {
      tenDangNhap: studentUsername,
      matKhau: studentPassword,
      email: randomEmail(),
      hoTen: 'Student Retaker',
      idVaiTro: 3, // Student role
      maNhanVien: `NV_${Date.now().toString().slice(-6)}`,
      chucDanh: 'Học viên',
      khoaPhong: 'Khoa Ngoại',
      trangThai: true
    })
    assert(res.data.success, `Create student failed: ${res.data.message}`)
    studentUserId = res.data.data.id
    console.log("  [DEBUG] Created student user ID:", studentUserId)
  })

  // 5. Assign candidate to the exams
  await test('RETAKE-005: Assign candidate to exams', async () => {
    await api.post('/examassignment/assign', {
      examId: examAId,
      userIds: [studentUserId],
      note: 'Retake test assign A',
    })
    await api.post('/examassignment/assign', {
      examId: examBId,
      userIds: [studentUserId],
      note: 'Retake test assign B',
    })
    await api.post('/examassignment/assign', {
      examId: examCId,
      userIds: [studentUserId],
      note: 'Retake test assign C',
    })
  })

  // 6. Login as Student
  await test('RETAKE-006: Login as student', async () => {
    // Wait for rate limit safety just in case
    await sleep(2000)
    const res = await loginAsStudent(studentUsername, studentPassword)
    assert(res.success, 'Student login failed')
    setAuth(state.studentToken)
  })

  // Helper to start, answer and submit an exam
  async function completeExamAttempt(expectedExams) {
    try {
      // Start exam (using kyThiId to let the system resolve, and passing dummy maDeThi to satisfy model validation)
      const startRes = await api.post('/exam/start', { kyThiId: kyThiId, maDeThi: 'DUMMY_CODE' })
      assert(startRes.data.success, `Start failed: ${startRes.data.message}`)
      
      const assignedExamId = startRes.data.data.idDeThi
      const baithiId = startRes.data.data.id
      console.log(`  [DEBUG] Started exam: baithiId=${baithiId}, assignedExamId=${assignedExamId}`)
      
      assert(expectedExams.includes(assignedExamId), `Expected one of [${expectedExams}] but got ${assignedExamId}`)

      // Get questions
      const qRes = await api.get(`/exam/${baithiId}/questions`)
      assert(qRes.data.success, 'Failed to get questions')
      const q = qRes.data.data[0]

      // Submit exam
      const submitRes = await api.post('/exam/submit', {
        idBaiThi: baithiId,
        danhSachCauTraLoi: [{
          idBaiThi: baithiId,
          idCauHoi: q.id,
          idLuaChonDaChon: q.danhSachLuaChon[0].id,
          daLuu: true,
        }],
      })
      assert(submitRes.data.success, `Submit failed: ${submitRes.data.message}`)
      return assignedExamId
    } catch (err) {
      console.error("  [DEBUG] completeExamAttempt error:", err.response?.data || err.message)
      throw err
    }
  }

  // --- RUN RETAKE CYCLE ---
  let firstExamId = 0
  let secondExamId = 0
  let thirdExamId = 0

  await test('RETAKE-007: First attempt', async () => {
    firstExamId = await completeExamAttempt([examAId, examBId, examCId])
  })

  await test('RETAKE-008: Second attempt (should assign a different exam)', async () => {
    const remainingExams = [examAId, examBId, examCId].filter(id => id !== firstExamId)
    secondExamId = await completeExamAttempt(remainingExams)
  })

  await test('RETAKE-009: Third attempt (should assign the last untaken exam)', async () => {
    const remainingExams = [examAId, examBId, examCId].filter(id => id !== firstExamId && id !== secondExamId)
    thirdExamId = await completeExamAttempt(remainingExams)
  })

  await test('RETAKE-010: Fourth attempt (all exams taken once, should repeat the first one)', async () => {
    const fourthExamId = await completeExamAttempt([firstExamId])
    assertEqual(fourthExamId, firstExamId, 'Should repeat the first exam after all have been taken')
  })

  // Restore admin auth for cleanup
  setAuth(state.adminToken)

  // CLEANUP
  await test('RETAKE-011: Cleanup test data', async () => {
    // Change status back to DangChuanBi to allow deletion of KyThi
    await api.post(`/kythi/${kyThiId}/status`, JSON.stringify('DangChuanBi'), {
      headers: { 'Content-Type': 'application/json' },
    })
    await api.delete(`/kythi/${kyThiId}`)
    await api.delete(`/exam/${examAId}`)
    await api.delete(`/exam/${examBId}`)
    await api.delete(`/exam/${examCId}`)
  })

  return printSummary('Retake Exams Cycle')
}

if (process.argv[1]?.includes('25-retake-exams')) {
  runRetakeTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
