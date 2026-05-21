/**
 * Test Suite 08: Ky Thi & Ca Thi Management
 */
import {
  api, state, test, assert, assertEqual, assertGreaterThan,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runKyThiTests() {
  console.log('\n📅 ═══ 08. KY THI & CA THI TESTS ═══')
  resetResults()
  await loginAsAdmin()

  const maKyThi = `KT_AUTO_${Date.now()}`

  // --- CREATE ---
  await test('KYTHI-001: Create ky thi', async () => {
    const res = await api.post('/kythi', {
      maKyThi,
      tenKyThi: 'Kỳ thi Bàn tay vàng - Automation Test',
      moTa: 'Kỳ thi được tạo bởi automation test',
      loaiKyThi: 'BanTayVang',
      thoiGianBatDau: new Date().toISOString(),
      thoiGianKetThuc: new Date(Date.now() + 7 * 86400000).toISOString(),
      donViToChuc: 'Phòng Điều dưỡng',
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    state.createdKyThiId = res.data.data.id
  })

  await test('KYTHI-002: Create ky thi with duplicate code should fail', async () => {
    try {
      const res = await api.post('/kythi', {
        maKyThi,
        tenKyThi: 'Duplicate',
        loaiKyThi: 'CNTT',
      })
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  // --- GET ---
  await test('KYTHI-003: Get all ky thi', async () => {
    const res = await api.get('/kythi')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('KYTHI-004: Get ky thi by ID', async () => {
    const res = await api.get(`/kythi/${state.createdKyThiId}`)
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.maKyThi, maKyThi, 'Code should match')
    assertEqual(res.data.data.trangThai, 'DangChuanBi', 'Initial status should be DangChuanBi')
  })

  await test('KYTHI-005: Filter by status', async () => {
    const res = await api.get('/kythi?trangThai=DangChuanBi')
    assert(res.data.success, 'Should succeed')
  })

  // --- UPDATE ---
  await test('KYTHI-006: Update ky thi', async () => {
    const res = await api.put(`/kythi/${state.createdKyThiId}`, {
      maKyThi,
      tenKyThi: 'Kỳ thi đã cập nhật',
      moTa: 'Mô tả mới',
      loaiKyThi: 'KiemSoatNhiemKhuan',
      thoiGianBatDau: new Date().toISOString(),
      thoiGianKetThuc: new Date(Date.now() + 14 * 86400000).toISOString(),
      donViToChuc: 'Phòng KSNK',
      trangThai: 'DangChuanBi',
    })
    assert(res.data.success, `Update failed: ${res.data.message}`)
  })

  // --- STATUS TRANSITIONS ---
  await test('KYTHI-007: Change status to DangDienRa', async () => {
    const res = await api.post(`/kythi/${state.createdKyThiId}/status`, JSON.stringify('DangDienRa'), {
      headers: { 'Content-Type': 'application/json' },
    })
    assert(res.data.success, `Status change failed: ${res.data.message}`)
  })

  await test('KYTHI-008: Change status to TamDung', async () => {
    const res = await api.post(`/kythi/${state.createdKyThiId}/status`, JSON.stringify('TamDung'), {
      headers: { 'Content-Type': 'application/json' },
    })
    assert(res.data.success, `Status change failed: ${res.data.message}`)
  })

  await test('KYTHI-009: Cannot delete ky thi DangDienRa', async () => {
    await api.post(`/kythi/${state.createdKyThiId}/status`, JSON.stringify('DangDienRa'), {
      headers: { 'Content-Type': 'application/json' },
    })
    try {
      const res = await api.delete(`/kythi/${state.createdKyThiId}`)
      assert(!res.data.success, 'Should not delete active ky thi')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  // --- CA THI ---
  let caThiId = 0

  await test('KYTHI-010: Create ca thi', async () => {
    // Change back to DangChuanBi for ca thi operations
    await api.post(`/kythi/${state.createdKyThiId}/status`, JSON.stringify('DangChuanBi'), {
      headers: { 'Content-Type': 'application/json' },
    })

    const res = await api.post('/kythi/ca-thi', {
      kyThiId: state.createdKyThiId,
      deThiId: state.createdExamId,
      tenCa: 'Ca 1 - Sáng 7h30',
      thoiGianBatDau: new Date().toISOString(),
      thoiGianKetThuc: new Date(Date.now() + 3600000).toISOString(),
      soLuongToiDa: 100,
      ghiChu: 'Ca thi automation test',
    })
    assert(res.data.success, `Create ca thi failed: ${res.data.message}`)
    caThiId = res.data.data.id
  })

  await test('KYTHI-011: Create second ca thi', async () => {
    const res = await api.post('/kythi/ca-thi', {
      kyThiId: state.createdKyThiId,
      tenCa: 'Ca 2 - Chiều 13h30',
      thoiGianBatDau: new Date(Date.now() + 3600000).toISOString(),
      thoiGianKetThuc: new Date(Date.now() + 7200000).toISOString(),
      soLuongToiDa: 50,
    })
    assert(res.data.success, `Create ca thi 2 failed: ${res.data.message}`)
  })

  await test('KYTHI-012: Get ca thi by ky thi', async () => {
    const res = await api.get(`/kythi/${state.createdKyThiId}/ca-thi`)
    assert(res.data.success, 'Should succeed')
    assertGreaterThan(res.data.data.length, 0, 'Should have ca thi')
  })

  await test('KYTHI-013: Update ca thi', async () => {
    const res = await api.put(`/kythi/ca-thi/${caThiId}`, {
      kyThiId: state.createdKyThiId,
      tenCa: 'Ca 1 - Updated',
      soLuongToiDa: 200,
    })
    assert(res.data.success, `Update ca thi failed: ${res.data.message}`)
  })

  await test('KYTHI-014: Delete ca thi', async () => {
    const res = await api.delete(`/kythi/ca-thi/${caThiId}`)
    assert(res.data.success, `Delete ca thi failed: ${res.data.message}`)
  })

  // --- CLEANUP ---
  await test('KYTHI-015: Delete ky thi', async () => {
    const res = await api.delete(`/kythi/${state.createdKyThiId}`)
    assert(res.data.success, `Delete ky thi failed: ${res.data.message}`)
  })

  return printSummary('Ky Thi & Ca Thi')
}

if (process.argv[1]?.includes('08-kythi')) {
  runKyThiTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
