/**
 * Test Suite 26: Department Manager Scoping & Security Verification
 */
import {
  api, state, setAuth, test, assert, assertEqual,
  loginAsAdmin, printSummary, resetResults, toLocalISOString,
  randomUsername, randomEmail
} from '../lib/test-helper.mjs'

export async function runDeptManagerTests() {
  console.log('\n🏢 ═══ 26. DEPARTMENT MANAGER SCOPING & SECURITY TESTS ═══')
  resetResults()

  // 1. Setup Phase: Create two managers and two questions in different departments
  let managerNoiId = 0
  let managerNgoaiId = 0
  let managerNoiToken = ''
  let managerNgoaiToken = ''

  let qNoiId = 0
  let qNgoaiId = 0
  let kyThiNoiId = 0

  const uNoi = randomUsername()
  const uNgoai = randomUsername()

  // Login as admin to setup accounts and questions
  await loginAsAdmin()

  await test('DM-001: Create Department Managers (Khoa Nội & Khoa Ngoại)', async () => {
    // Manager for Khoa Nội (ID = 1)
    const resNoi = await api.post('/user', {
      tenDangNhap: uNoi,
      matKhau: 'Manager@123',
      email: randomEmail(),
      hoTen: 'Quản lý Khoa Nội',
      chucDanh: 'Trưởng khoa',
      khoaPhong: 'Khoa Nội',
      idKhoaQuanLy: 1, // Khoa Nội
      idVaiTro: 5, // Quản lý Khoa
      trangThai: true,
    })
    assert(resNoi.data.success, `Create manager Noi failed: ${resNoi.data.message}`)
    managerNoiId = resNoi.data.data.id

    // Manager for Khoa Ngoại (ID = 2)
    const resNgoai = await api.post('/user', {
      tenDangNhap: uNgoai,
      matKhau: 'Manager@123',
      email: randomEmail(),
      hoTen: 'Quản lý Khoa Ngoại',
      chucDanh: 'Trưởng khoa',
      khoaPhong: 'Khoa Ngoại',
      idKhoaQuanLy: 2, // Khoa Ngoại
      idVaiTro: 5, // Quản lý Khoa
      trangThai: true,
    })
    assert(resNgoai.data.success, `Create manager Ngoai failed: ${resNgoai.data.message}`)
    managerNgoaiId = resNgoai.data.data.id
  })

  await test('DM-002: Create questions in respective departments', async () => {
    // Question for Khoa Nội
    const resQNoi = await api.post('/cauhoi', {
      noiDung: `Câu hỏi Khoa Nội - Auto Test ${Date.now()}`,
      idLoaiCauHoi: 1,
      doKho: 'De',
      diem: 1,
      khoaPhong: 'Khoa Nội',
      danhSachLuaChon: [
        { noiDung: 'Đúng', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Sai', thuTu: 2, laDapAnDung: false },
      ],
    })
    assert(resQNoi.data.success, `Create question Noi failed: ${resQNoi.data.message}`)
    qNoiId = resQNoi.data.data.id

    // Question for Khoa Ngoại
    const resQNgoai = await api.post('/cauhoi', {
      noiDung: `Câu hỏi Khoa Ngoại - Auto Test ${Date.now()}`,
      idLoaiCauHoi: 1,
      doKho: 'De',
      diem: 1,
      khoaPhong: 'Khoa Ngoại',
      danhSachLuaChon: [
        { noiDung: 'Đúng', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Sai', thuTu: 2, laDapAnDung: false },
      ],
    })
    assert(resQNgoai.data.success, `Create question Ngoai failed: ${resQNgoai.data.message}`)
    qNgoaiId = resQNgoai.data.data.id
  })

  // 2. Authentication Phase
  await test('DM-003: Login as both Department Managers', async () => {
    const resNoi = await api.post('/auth/login', { username: uNoi, password: 'Manager@123' })
    assert(resNoi.data.success, 'Login as manager Noi failed')
    managerNoiToken = resNoi.data.data.accessToken

    const resNgoai = await api.post('/auth/login', { username: uNgoai, password: 'Manager@123' })
    assert(resNgoai.data.success, 'Login as manager Ngoai failed')
    managerNgoaiToken = resNgoai.data.data.accessToken
  })

  // 3. KyThi Scoping Verification
  await test('DM-004: Manager cannot create KyThi for another department', async () => {
    setAuth(managerNoiToken)
    const res = await api.post('/kythi', {
      maKyThi: `KT_NOI_${Date.now()}`,
      tenKyThi: 'Kỳ thi thử nghiệm Khoa Nội',
      moTa: 'Tạo bởi quản lý khoa nội',
      khoaPhongId: 2, // Try to set Khoa Ngoại (ID = 2)
      thoiGianBatDau: toLocalISOString(new Date(Date.now() + 3600000)), // 1 hour in future
      thoiGianKetThuc: toLocalISOString(new Date(Date.now() + 4 * 3600000)), // 4 hours in future (3 hours duration)
    })
    assert(res.data.success, 'Request should succeed but must overwrite department ID')
    assertEqual(res.data.data.khoaPhongId, 1, 'Department ID must be forced to Manager own department (1)')
    kyThiNoiId = res.data.data.id
  })

  await test('DM-005: Manager cannot retrieve KyThi of another department', async () => {
    setAuth(managerNgoaiToken) // Manager Ngoại (ID = 2)
    try {
      await api.get(`/kythi/${kyThiNoiId}`)
      assert(false, 'Should have thrown Forbidden error')
    } catch (err) {
      assertEqual(err.response?.status, 403, 'Should get 403 Forbidden')
    }
  })

  await test('DM-006: Manager cannot update KyThi of another department', async () => {
    setAuth(managerNgoaiToken)
    try {
      const res = await api.put(`/kythi/${kyThiNoiId}`, {
        maKyThi: `KT_NOI_${Date.now()}`,
        tenKyThi: 'Cố gắng sửa đổi',
        moTa: 'Sửa đổi mô tả',
        khoaPhongId: 2,
        thoiGianBatDau: toLocalISOString(new Date(Date.now() + 3600000)),
        thoiGianKetThuc: toLocalISOString(new Date(Date.now() + 4 * 3600000)),
        donViToChuc: 'Khoa Ngoại',
      })
      console.log('Update call succeeded unexpectedly:', res.status, res.data)
      assert(false, 'Should have thrown Forbidden error')
    } catch (err) {
      console.log('DM-006 error response:', err.response?.status, err.response?.data)
      assertEqual(err.response?.status, 403, 'Should get 403 Forbidden')
    }
  })

  await test('DM-007: Manager cannot delete KyThi of another department', async () => {
    setAuth(managerNgoaiToken)
    try {
      await api.delete(`/kythi/${kyThiNoiId}`)
      assert(false, 'Should have thrown Forbidden error')
    } catch (err) {
      assertEqual(err.response?.status, 403, 'Should get 403 Forbidden')
    }
  })

  // 4. Exam & Questions Scoping Verification
  await test('DM-008: Manager cannot link exam to another department KyThi', async () => {
    setAuth(managerNgoaiToken) // manager_ngoai tries to link to kyThiNoiId (dept 1)
    try {
      await api.post('/exam', {
        maDeThi: `DE_NGOAI_${Date.now()}`,
        tenDeThi: 'Đề thi khoa ngoại',
        thoiGianLamBai: 45,
        thoiGianBatDau: toLocalISOString(new Date(Date.now() + 3600000)),
        trangThai: 'Draft',
        kyThiId: kyThiNoiId, // KyThi of Khoa Nội
        danhSachIdCauHoi: [qNgoaiId],
      })
      assert(false, 'Should have thrown BadRequest error')
    } catch (err) {
      assertEqual(err.response?.status, 400, 'Should get 400 BadRequest')
      assert(err.response?.data?.message?.includes('kỳ thi của khoa khác'), 'Error message should explain target KyThi is mismatch')
    }
  })

  await test('DM-009: Manager cannot add questions of another department to their manual exam', async () => {
    setAuth(managerNoiToken) // manager_noi tries to add qNgoaiId
    try {
      await api.post('/exam', {
        maDeThi: `DE_NOI_${Date.now()}`,
        tenDeThi: 'Đề thi khoa nội chứa câu hỏi khoa ngoại',
        thoiGianLamBai: 45,
        thoiGianBatDau: toLocalISOString(new Date(Date.now() + 3600000)),
        trangThai: 'Draft',
        danhSachIdCauHoi: [qNoiId, qNgoaiId], // Contains Khoa Ngoại question!
      })
      assert(false, 'Should have thrown BadRequest error')
    } catch (err) {
      assertEqual(err.response?.status, 400, 'Should get 400 BadRequest')
      assert(err.response?.data?.message?.includes('thuộc về khoa của người quản lý'), 'Error message should explain question department mismatch')
    }
  })

  await test('DM-010: Manager successfully creates exam with their own questions', async () => {
    setAuth(managerNoiToken)
    try {
      const res = await api.post('/exam', {
        maDeThi: `DE_NOI_OK_${Date.now()}`,
        tenDeThi: 'Đề thi khoa nội hợp lệ',
        thoiGianLamBai: 45,
        thoiGianBatDau: toLocalISOString(new Date(Date.now() + 3600000)),
        trangThai: 'Draft',
        kyThiId: kyThiNoiId,
        danhSachIdCauHoi: [qNoiId],
      })
      assert(res.data.success, `Create exam failed: ${res.data.message}`)
      assertEqual(res.data.data.khoaPhong, 'Khoa Nội', 'Exam must be created under manager department')
    } catch (err) {
      console.log('DM-010 error response:', err.response?.status, err.response?.data)
      throw err;
    }
  })

  // 5. Cleanup Phase
  await test('DM-011: Cleanup test resources', async () => {
    await loginAsAdmin()

    // Delete KyThi
    if (kyThiNoiId > 0) {
      await api.delete(`/kythi/${kyThiNoiId}`)
    }

    // Delete Questions
    if (qNoiId > 0) await api.delete(`/cauhoi/${qNoiId}`)
    if (qNgoaiId > 0) await api.delete(`/cauhoi/${qNgoaiId}`)

    // Delete Users
    if (managerNoiId > 0) await api.delete(`/user/${managerNoiId}`)
    if (managerNgoaiId > 0) await api.delete(`/user/${managerNgoaiId}`)
  })

  return printSummary('Department Manager Scoping')
}

if (process.argv[1]?.includes('26-dept-manager')) {
  runDeptManagerTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
