/**
 * Test Suite 24: All Departments Exam Accessibility for Candidates
 */
import {
  api, state, setAuth, test, assert, assertEqual,
  loginAsAdmin, printSummary, resetResults, toLocalISOString,
  randomUsername, randomEmail
} from '../lib/test-helper.mjs'

export async function runAllDeptExamTests() {
  console.log('\n📅 ═══ 24. ALL DEPARTMENTS EXAM ACCESSIBILITY TESTS ═══')
  resetResults()
  await loginAsAdmin()

  const tempStudentUsername = randomUsername()
  const tempStudentEmail = randomEmail()
  let studentUserId = 0
  let studentToken = ''

  const maKyThiAllDepts = `KT_ALL_${Date.now()}`
  let kyThiId = 0

  // 1. Create a student belonging to 'Khoa Nội Tổng Hợp'
  await test('ALLDEPT-001: Create student with department', async () => {
    const res = await api.post('/user', {
      tenDangNhap: tempStudentUsername,
      matKhau: 'Student@123',
      email: tempStudentEmail,
      hoTen: 'Student Department Test',
      maNhanVien: `NV_${Date.now()}`,
      chucDanh: 'Điều dưỡng',
      khoaPhong: 'Khoa Nội Tổng Hợp',
      idVaiTro: 3,
      trangThai: true,
    })
    assert(res.data.success, `Create student failed: ${res.data.message}`)
    studentUserId = res.data.data.id
  })

  // 2. Create a KyThi for all departments (khoaPhongId = null)
  await test('ALLDEPT-002: Create KyThi with null department (All departments)', async () => {
    const res = await api.post('/kythi', {
      maKyThi: maKyThiAllDepts,
      tenKyThi: 'Kỳ thi cho tất cả khoa phòng - Automation Test',
      moTa: 'Kỳ thi cho tất cả khoa phòng',
      khoaPhongId: null,
      thoiGianBatDau: toLocalISOString(new Date(Date.now() + 60000)), // 1 minute in the future (Local)
      thoiGianKetThuc: toLocalISOString(new Date(Date.now() + 7 * 86400000)), // Local
      donViToChuc: '',
    })
    assert(res.data.success, `Create KyThi failed: ${res.data.message}`)
    kyThiId = res.data.data.id
  })

  // 3. Login as the student
  await test('ALLDEPT-003: Login as student with department', async () => {
    const res = await api.post('/auth/login', { username: tempStudentUsername, password: 'Student@123' })
    assert(res.data.success, `Login failed: ${res.data.message}`)
    studentToken = res.data.data.accessToken
    setAuth(studentToken)
  })

  // 4. Retrieve all KyThi list for student and check if our KyThi is present
  await test('ALLDEPT-004: Student should see the all-department KyThi', async () => {
    const res = await api.get('/kythi')
    assert(res.data.success, `Get KyThi list failed: ${res.data.message}`)
    const kyThiList = res.data.data
    const found = kyThiList.some(k => k.id === kyThiId)
    assert(found, 'Student should see the KyThi created with null department')
  })

  // 5. Cleanup
  await test('ALLDEPT-005: Cleanup created KyThi and User', async () => {
    // Restore admin token
    await loginAsAdmin()
    
    // Delete KyThi
    if (kyThiId > 0) {
      const delKyThi = await api.delete(`/kythi/${kyThiId}`)
      assert(delKyThi.data.success, 'Should delete KyThi successfully')
    }

    // Delete User
    if (studentUserId > 0) {
      const delUser = await api.delete(`/user/${studentUserId}`)
      assert(delUser.data.success, 'Should delete user successfully')
    }
  })

  return printSummary('All Dept Exam Accessibility')
}

if (process.argv[1]?.includes('24-alldept-exam')) {
  runAllDeptExamTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
