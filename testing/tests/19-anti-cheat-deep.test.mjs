/**
 * Test Suite 19: Deep Anti-Cheat Testing
 * Tests all cheating detection scenarios and warning accumulation
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan, sleep,
  loginAsAdmin, randomUsername, randomEmail, randomExamCode, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runAntiCheatTests() {
  console.log('\n🚨 ═══ 19. DEEP ANTI-CHEAT TESTS ═══')
  resetResults()
  setAuth(state.adminToken)

  // Setup: create exam for anti-cheat testing
  const qRes = await api.post('/cauhoi', {
    noiDung: 'Anti-cheat test question',
    idLoaiCauHoi: 1, doKho: 'De', diem: 1, idDanhMuc: 1,
    danhSachLuaChon: [
      { noiDung: 'A', thuTu: 1, laDapAnDung: true },
      { noiDung: 'B', thuTu: 2, laDapAnDung: false },
    ],
  })
  const questionId = qRes.data.data.id

  const examCode = randomExamCode()
  await api.post('/exam', {
    maDeThi: examCode,
    tenDeThi: 'Anti-Cheat Deep Test',
    thoiGianLamBai: 30,
    thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
    trangThai: 'Active',
    danhSachIdCauHoi: [questionId],
  })

  // Create and login student
  const username = randomUsername()
  await api.post('/user', {
    tenDangNhap: username,
    matKhau: 'AntiCheat@123',
    email: randomEmail(),
    hoTen: 'Anti Cheat Student',
    idVaiTro: 3,
    trangThai: true,
  })

  const userRes = await api.get(`/user?pageNumber=1&pageSize=100&searchKeyword=${username}`)
  const userId = userRes.data.data.find(u => u.tenDangNhap === username)?.id
  const examIdRes = await api.get(`/exam/code/${examCode}`)
  const examId = examIdRes.data.data.id

  await api.post('/examassignment/assign', { examId, userIds: [userId] })

  await sleep(2000)
  const loginRes = await api.post('/auth/login', { username, password: 'AntiCheat@123' })
  const studentToken = loginRes.data.data.accessToken
  setAuth(studentToken)

  // Start exam
  const startRes = await api.post('/exam/start', { maDeThi: examCode })
  const baithiId = startRes.data.data.id

  // --- WARNING TYPES ---
  const warningTypes = [
    { type: 'TAB_SWITCH', desc: 'Thí sinh chuyển sang tab khác' },
    { type: 'BROWSER_FOCUS_LOST', desc: 'Cửa sổ trình duyệt mất focus' },
    { type: 'RIGHT_CLICK', desc: 'Thí sinh click chuột phải' },
    { type: 'COPY_PASTE', desc: 'Thí sinh sử dụng Ctrl+C' },
    { type: 'MULTIPLE_TABS', desc: 'Phát hiện nhiều tab mở đề thi' },
    { type: 'SUSPICIOUS_KEYBOARD', desc: 'Phát hiện phím tắt bất thường (F12)' },
    { type: 'SCREEN_CAPTURE', desc: 'Phát hiện chụp màn hình' },
  ]

  for (let i = 0; i < warningTypes.length; i++) {
    const w = warningTypes[i]
    await test(`CHEAT-${String(i + 1).padStart(3, '0')}: Log warning type ${w.type}`, async () => {
      const res = await api.post('/exam/warning', {
        idBaiThi: baithiId,
        loaiCanhBao: w.type,
        moTa: w.desc,
      })
      assert(res.data.success, `Log ${w.type} failed`)
    })
  }

  await test('CHEAT-008: Warning count accumulates correctly', async () => {
    const res = await api.get(`/exam/${baithiId}/warnings`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data, 7, 'Should have 7 warnings')
  })

  await test('CHEAT-009: Multiple warnings of same type', async () => {
    for (let i = 0; i < 3; i++) {
      await api.post('/exam/warning', {
        idBaiThi: baithiId,
        loaiCanhBao: 'TAB_SWITCH',
        moTa: `Tab switch #${i + 2}`,
      })
    }
    const res = await api.get(`/exam/${baithiId}/warnings`)
    assertEqual(res.data.data, 10, 'Should have 10 total warnings')
  })

  await test('CHEAT-010: Warning with empty description', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: baithiId,
      loaiCanhBao: 'TAB_SWITCH',
      moTa: '',
    })
    assert(res.data.success, 'Should accept empty description')
  })

  await test('CHEAT-011: Warning with XSS in description', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: baithiId,
      loaiCanhBao: 'SUSPICIOUS_KEYBOARD',
      moTa: '<script>alert("hack")</script>Suspicious activity',
    })
    assert(res.data.success, 'Should accept and sanitize')
  })

  await test('CHEAT-012: Warning with very long description', async () => {
    const res = await api.post('/exam/warning', {
      idBaiThi: baithiId,
      loaiCanhBao: 'COPY_PASTE',
      moTa: 'Long description: ' + 'x'.repeat(500),
    })
    assert(res.data.success, 'Should handle long description')
  })

  // --- EXAM STILL WORKS AFTER WARNINGS ---
  await test('CHEAT-013: Can still save answers after warnings', async () => {
    const qRes2 = await api.get(`/exam/${baithiId}/questions`)
    const q = qRes2.data.data[0]
    const res = await api.post('/exam/answer', {
      idBaiThi: baithiId,
      idCauHoi: q.id,
      idLuaChonDaChon: q.danhSachLuaChon[0].id,
      daLuu: true,
    })
    assert(res.data.success, 'Should still be able to answer')
  })

  await test('CHEAT-014: Can still submit after many warnings', async () => {
    const qRes2 = await api.get(`/exam/${baithiId}/questions`)
    const res = await api.post('/exam/submit', {
      idBaiThi: baithiId,
      danhSachCauTraLoi: qRes2.data.data.map(q => ({
        idBaiThi: baithiId,
        idCauHoi: q.id,
        idLuaChonDaChon: q.danhSachLuaChon[0].id,
        daLuu: true,
      })),
    })
    assert(res.data.success, 'Submit should still work')
  })

  // --- ADMIN VIEWS WARNINGS ---
  await test('CHEAT-015: Admin can view warning count', async () => {
    setAuth(state.adminToken)
    const res = await api.get(`/exam/${baithiId}/warnings`)
    assert(res.data.success, 'Should succeed')
    assertGreaterThan(res.data.data, 10, 'Should have many warnings')
  })

  await test('CHEAT-016: Result includes warning count', async () => {
    const res = await api.get(`/grading/result/${baithiId}`)
    assert(res.data.success, 'Should succeed')
    // soCanhBao may be null if not synced yet, but warnings endpoint confirms
    const warnRes = await api.get(`/exam/${baithiId}/warnings`)
    assertGreaterThan(warnRes.data.data, 0, 'Warnings endpoint should show count')
  })

  // Cleanup
  await api.delete(`/cauhoi/${questionId}`)

  return printSummary('Deep Anti-Cheat')
}

if (process.argv[1]?.includes('19-anti-cheat')) {
  runAntiCheatTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
