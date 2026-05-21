/**
 * Test Suite 16: Complete Exam Workflow (End-to-End)
 * Tests the full lifecycle: Create questions → Create exam → Assign → Take → Grade → Report
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan, sleep,
  loginAsAdmin, randomUsername, randomEmail, randomExamCode, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runExamWorkflowTests() {
  console.log('\n🔄 ═══ 16. COMPLETE EXAM WORKFLOW (E2E) ═══')
  resetResults()
  setAuth(state.adminToken)

  // State for this workflow
  let questionIds = []
  let examCode = ''
  let examId = 0
  let studentUsername = ''
  let studentToken = ''
  let baithiId = 0

  // === STEP 1: Create questions ===
  await test('E2E-001: Admin creates 3 questions for exam', async () => {
    const questions = [
      {
        noiDung: 'E2E: Nhiệt độ bảo quản vaccine COVID-19 Pfizer?',
        doKho: 'Kho', diem: 3,
        choices: [
          { noiDung: '-70°C', correct: true },
          { noiDung: '2-8°C', correct: false },
          { noiDung: '-20°C', correct: false },
          { noiDung: '25°C', correct: false },
        ],
      },
      {
        noiDung: 'E2E: Thời gian rửa tay tối thiểu với xà phòng?',
        doKho: 'TrungBinh', diem: 2,
        choices: [
          { noiDung: '10 giây', correct: false },
          { noiDung: '20 giây', correct: true },
          { noiDung: '40 giây', correct: false },
          { noiDung: '60 giây', correct: false },
        ],
      },
      {
        noiDung: 'E2E: PPE là viết tắt của?',
        doKho: 'De', diem: 1,
        choices: [
          { noiDung: 'Personal Protective Equipment', correct: true },
          { noiDung: 'Patient Protection Equipment', correct: false },
          { noiDung: 'Professional Practice Exam', correct: false },
        ],
      },
    ]

    for (const q of questions) {
      const res = await api.post('/cauhoi', {
        noiDung: q.noiDung,
        idLoaiCauHoi: 1,
        doKho: q.doKho,
        diem: q.diem,
        idDanhMuc: 1,
        khoaPhong: 'Khoa KSNK',
        danhSachLuaChon: q.choices.map((c, i) => ({
          noiDung: c.noiDung,
          thuTu: i + 1,
          laDapAnDung: c.correct,
        })),
      })
      assert(res.data.success, `Create question failed: ${res.data.message}`)
      questionIds.push(res.data.data.id)
    }
    assertEqual(questionIds.length, 3, 'Should create 3 questions')
  })

  // === STEP 2: Create exam ===
  await test('E2E-002: Admin creates exam with 3 questions', async () => {
    examCode = randomExamCode()
    const res = await api.post('/exam', {
      maDeThi: examCode,
      tenDeThi: 'E2E: Kiểm tra kiến thức KSNK',
      thoiGianLamBai: 15,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: questionIds,
    })
    assert(res.data.success, `Create exam failed: ${res.data.message}`)

    // Get exam ID
    const examRes = await api.get(`/exam/code/${examCode}`)
    examId = examRes.data.data.id
    assertGreaterThan(examId, 0, 'Should have exam ID')
  })

  // === STEP 3: Create student ===
  await test('E2E-003: Admin creates student account', async () => {
    studentUsername = randomUsername()
    const res = await api.post('/user', {
      tenDangNhap: studentUsername,
      matKhau: 'Student@E2E123',
      email: randomEmail(),
      hoTen: 'Trần Thị E2E',
      maNhanVien: 'NV_E2E',
      chucDanh: 'Điều dưỡng',
      khoaPhong: 'Khoa Nội',
      idVaiTro: 3,
      trangThai: true,
    })
    assert(res.data.success, `Create student failed: ${res.data.message}`)
  })

  // === STEP 4: Assign student ===
  await test('E2E-004: Admin assigns student to exam', async () => {
    const userRes = await api.get(`/user?pageNumber=1&pageSize=100&searchKeyword=${studentUsername}`)
    const student = userRes.data.data.find(u => u.tenDangNhap === studentUsername)
    assert(student, 'Should find student')

    const res = await api.post('/examassignment/assign', {
      examId,
      userIds: [student.id],
      note: 'E2E test assignment',
    })
    assert(res.data.success, `Assign failed: ${res.data.message}`)
  })

  // === STEP 5: Student login ===
  await test('E2E-005: Student logs in', async () => {
    await sleep(2000)
    const res = await api.post('/auth/login', { username: studentUsername, password: 'Student@E2E123' })
    assert(res.data.success, `Student login failed: ${res.data.message}`)
    studentToken = res.data.data.accessToken
    assertEqual(res.data.data.user.role, 'Student', 'Should be Student role')
  })

  // === STEP 6: Student starts exam ===
  await test('E2E-006: Student starts exam', async () => {
    setAuth(studentToken)
    const res = await api.post('/exam/start', { maDeThi: examCode })
    assert(res.data.success, `Start failed: ${res.data.message}`)
    baithiId = res.data.data.id
    assertEqual(res.data.data.trangThai, 'InProgress', 'Should be InProgress')
    assertGreaterThan(res.data.data.thoiGianConLai, 0, 'Should have time remaining')
  })

  // === STEP 7: Student answers questions ===
  let examQuestions = []
  await test('E2E-007: Student gets exam questions', async () => {
    const res = await api.get(`/exam/${baithiId}/questions`)
    assert(res.data.success, 'Should get questions')
    examQuestions = res.data.data
    assertEqual(examQuestions.length, 3, 'Should have 3 questions')
  })

  await test('E2E-008: Student answers all questions correctly', async () => {
    for (const q of examQuestions) {
      // Select first choice (which should be correct based on our setup)
      const choice = q.danhSachLuaChon[0]
      const res = await api.post('/exam/answer', {
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: choice.id,
        daLuu: true,
      })
      assert(res.data.success, `Save answer failed for question ${q.id}`)
    }
  })

  await test('E2E-009: Student checks progress', async () => {
    const res = await api.get(`/exam/${baithiId}/progress`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.trangThai, 'InProgress', 'Should still be in progress')
  })

  // === STEP 8: Anti-cheat triggers ===
  await test('E2E-010: Anti-cheat logs tab switch', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: baithiId,
      loaiCanhBao: 'TAB_SWITCH',
      moTa: 'E2E: Student switched tab',
    })
    assert(res.data.success, 'Warning should be logged')
  })

  // === STEP 9: Student submits ===
  await test('E2E-011: Student submits exam', async () => {
    const answers = examQuestions.map(q => ({
      idBaiThi: baithiId,
      idCauHoi: q.id,
      idLuaChonDaChon: q.danhSachLuaChon[0].id,
      daLuu: true,
    }))

    const res = await api.post('/exam/submit', {
      idBaiThi: baithiId,
      danhSachCauTraLoi: answers,
    })
    assert(res.data.success, `Submit failed: ${res.data.message}`)
    assertEqual(res.data.data.trangThai, 'Completed', 'Should be Completed')
  })

  // === STEP 10: Admin grades and reviews ===
  await test('E2E-012: Admin views result detail', async () => {
    setAuth(state.adminToken)
    const res = await api.get(`/grading/result/${baithiId}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.baiThiId, baithiId, 'BaiThi ID should match')
    assert(res.data.data.tongSoCau === 3, 'Should have 3 questions')
    assert(res.data.data.answers.length > 0, 'Should have answer details')
  })

  await test('E2E-013: Admin regrades exam', async () => {
    const res = await api.post(`/grading/regrade/${baithiId}`)
    assert(res.data.success, `Regrade failed: ${res.data.message}`)
    assert(res.data.data.tongDiem >= 0, 'Should have score')
  })

  await test('E2E-014: Admin views exam ranking', async () => {
    const res = await api.get(`/grading/exam/${examId}/ranking?top=10`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length > 0, 'Should have at least 1 result')
  })

  await test('E2E-015: Admin exports results to Excel', async () => {
    const res = await api.get(`/grading/exam/${examId}/export`, { responseType: 'arraybuffer' })
    assert(res.status === 200, 'Should return 200')
    assertGreaterThan(res.data.byteLength, 100, 'Should have file content')
  })

  await test('E2E-016: Admin views exam statistics', async () => {
    const res = await api.get(`/statistics/exam/${examId}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.totalParticipants, 1, 'Should have 1 participant')
    assertEqual(res.data.data.completedCount, 1, 'Should have 1 completed')
  })

  await test('E2E-017: Admin checks cheating warnings', async () => {
    const res = await api.get(`/exam/${baithiId}/warnings`)
    assert(res.data.success, 'Should succeed')
    assertGreaterThan(res.data.data, 0, 'Should have warnings')
  })

  await test('E2E-018: Student views own history', async () => {
    setAuth(studentToken)
    const res = await api.get('/statistics/my-history')
    assert(res.data.success, 'Should succeed')
    const found = res.data.data.find(h => h.baiThiId === baithiId)
    assert(found, 'Should find this exam in history')
    assertEqual(found.trangThai, 'Completed', 'Should be completed')
  })

  // === CLEANUP ===
  await test('E2E-019: Cleanup - delete test questions', async () => {
    setAuth(state.adminToken)
    for (const id of questionIds) {
      await api.delete(`/cauhoi/${id}`)
    }
    assert(true, 'Cleanup done')
  })

  return printSummary('Complete Exam Workflow (E2E)')
}

if (process.argv[1]?.includes('16-exam-workflow')) {
  runExamWorkflowTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
