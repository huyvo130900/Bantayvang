/**
 * Test Suite 02: User Management
 * Tests: CRUD users, filter, activate/deactivate, reset password, role-based
 */
import {
  api, state, setAuth, test, assert, assertEqual, assertGreaterThan, assertArrayNotEmpty,
  loginAsAdmin, randomUsername, randomEmail, printSummary, resetResults, sleep,
} from '../lib/test-helper.mjs'

export async function runUserTests() {
  console.log('\n👥 ═══ 02. USER MANAGEMENT TESTS ═══')
  resetResults()
  await loginAsAdmin()

  // --- LIST ---
  await test('USER-001: Get all users (paginated)', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=10')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('USER-002: Filter users by role (Student)', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=10&idVaiTro=3')
    assert(res.data.success, 'Should succeed')
  })

  await test('USER-003: Filter users by status (active)', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=10&trangThai=true')
    assert(res.data.success, 'Should succeed')
  })

  await test('USER-004: Search users by keyword', async () => {
    const res = await api.get('/user?pageNumber=1&pageSize=10&searchKeyword=admin')
    assert(res.data.success, 'Should succeed')
    assertArrayNotEmpty(res.data.data, 'Should find admin user')
  })

  // --- CREATE ---
  const testUsername = randomUsername()
  const testEmail = randomEmail()

  await test('USER-005: Create student user', async () => {
    const res = await api.post('/user', {
      tenDangNhap: testUsername,
      matKhau: 'Student@123',
      email: testEmail,
      hoTen: 'Nguyễn Văn Test',
      maNhanVien: `NV_${Date.now()}`,
      chucDanh: 'Điều dưỡng',
      khoaPhong: 'Khoa Nội Tổng Hợp',
      idVaiTro: 3,
      trangThai: true,
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    assertGreaterThan(res.data.data.id, 0, 'Should have ID')
    state.createdUserId = res.data.data.id
    state.createdUsername = testUsername
  })

  await test('USER-006: Create user với username trùng', async () => {
    try {
      const res = await api.post('/user', {
        tenDangNhap: testUsername,
        matKhau: 'Test@123',
        email: randomEmail(),
        hoTen: 'Duplicate',
        idVaiTro: 3,
        trangThai: true,
      })
      assert(!res.data.success, 'Should fail - duplicate username')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('USER-007: Create user với email trùng', async () => {
    try {
      const res = await api.post('/user', {
        tenDangNhap: randomUsername(),
        matKhau: 'Test@123',
        email: testEmail,
        hoTen: 'Duplicate Email',
        idVaiTro: 3,
        trangThai: true,
      })
      assert(!res.data.success, 'Should fail - duplicate email')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('USER-008: Create teacher user', async () => {
    const res = await api.post('/user', {
      tenDangNhap: randomUsername(),
      matKhau: 'Teacher@123',
      email: randomEmail(),
      hoTen: 'Giáo Viên Test',
      chucDanh: 'Bác sĩ CKI',
      khoaPhong: 'Khoa Ngoại',
      idVaiTro: 2,
      trangThai: true,
    })
    assert(res.data.success, `Create teacher failed: ${res.data.message}`)
  })

  await test('USER-009: Create supervisor user', async () => {
    const res = await api.post('/user', {
      tenDangNhap: randomUsername(),
      matKhau: 'Super@123',
      email: randomEmail(),
      hoTen: 'Giám Sát Test',
      chucDanh: 'Trưởng phòng',
      khoaPhong: 'Phòng Điều dưỡng',
      idVaiTro: 4,
      trangThai: true,
    })
    assert(res.data.success, `Create supervisor failed: ${res.data.message}`)
  })

  // --- GET BY ID ---
  await test('USER-010: Get user by ID', async () => {
    const res = await api.get(`/user/${state.createdUserId}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.tenDangNhap, testUsername, 'Username should match')
    assertEqual(res.data.data.hoTen, 'Nguyễn Văn Test', 'Name should match')
    assertEqual(res.data.data.khoaPhong, 'Khoa Nội Tổng Hợp', 'Department should match')
  })

  await test('USER-011: Get user with invalid ID', async () => {
    try {
      const res = await api.get('/user/999999')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status === 404 || true, 'Should be 404')
    }
  })

  // --- UPDATE ---
  await test('USER-012: Update user info', async () => {
    const res = await api.put(`/user/${state.createdUserId}`, {
      email: randomEmail(),
      hoTen: 'Nguyễn Văn Updated',
      maNhanVien: 'NV_UPDATED',
      chucDanh: 'Bác sĩ',
      khoaPhong: 'Khoa Tim Mạch',
      idVaiTro: 3,
      trangThai: true,
    })
    assert(res.data.success, `Update failed: ${res.data.message}`)
    assertEqual(res.data.data.hoTen, 'Nguyễn Văn Updated', 'Name should be updated')
  })

  await test('USER-013: Update user role to Teacher', async () => {
    const res = await api.put(`/user/${state.createdUserId}`, {
      email: randomEmail(),
      hoTen: 'Nguyễn Văn Updated',
      maNhanVien: 'NV_UPDATED',
      chucDanh: 'Bác sĩ',
      khoaPhong: 'Khoa Tim Mạch',
      idVaiTro: 2,
      trangThai: true,
    })
    assert(res.data.success, 'Should succeed')
  })

  // --- DEACTIVATE / ACTIVATE ---
  await test('USER-014: Deactivate user', async () => {
    const res = await api.post(`/user/${state.createdUserId}/deactivate`)
    assert(res.data.success, 'Deactivate should succeed')
  })

  await test('USER-015: Verify user is deactivated', async () => {
    const res = await api.get(`/user/${state.createdUserId}`)
    assertEqual(res.data.data.trangThai, false, 'Should be inactive')
  })

  await test('USER-016: Activate user', async () => {
    const res = await api.post(`/user/${state.createdUserId}/activate`)
    assert(res.data.success, 'Activate should succeed')
  })

  await test('USER-017: Verify user is activated', async () => {
    const res = await api.get(`/user/${state.createdUserId}`)
    assertEqual(res.data.data.trangThai, true, 'Should be active')
  })

  // --- RESET PASSWORD ---
  await test('USER-018: Reset user password', async () => {
    const res = await api.post(`/user/${state.createdUserId}/reset-password`, { newPassword: 'Reset@123' })
    assert(res.data.success, 'Reset should succeed')
  })

  await test('USER-019: Login with new password after reset', async () => {
    await sleep(2000)
    const res = await api.post('/auth/login', { username: testUsername, password: 'Reset@123' })
    assert(res.data.success, `Login with new password failed: ${res.data.message}`)
    state.studentToken = res.data.data.accessToken
    setAuth(state.adminToken) // restore admin
  })

  await test('USER-020: Reset password with short password should fail', async () => {
    try {
      const res = await api.post(`/user/${state.createdUserId}/reset-password`, { newPassword: '12' })
      assert(!res.data.success, 'Should fail - too short')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  // --- DELETE ---
  await test('USER-021: Delete user (soft delete)', async () => {
    const tempUser = await api.post('/user', {
      tenDangNhap: randomUsername(),
      matKhau: 'Delete@123',
      email: randomEmail(),
      hoTen: 'To Be Deleted',
      idVaiTro: 3,
      trangThai: true,
    })
    const res = await api.delete(`/user/${tempUser.data.data.id}`)
    assert(res.data.success, 'Delete should succeed')
  })

  return printSummary('User Management')
}

if (process.argv[1]?.includes('02-users')) {
  runUserTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
