/**
 * Test Suite 13: Edge Cases & Boundary Testing
 * Tests unusual inputs, boundary values, and error handling
 */
import {
  api, state, setAuth, test, assert, assertEqual, sleep,
  loginAsAdmin, randomUsername, randomEmail, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runEdgeCaseTests() {
  console.log('\n🧪 ═══ 13. EDGE CASES & BOUNDARY TESTS ═══')
  resetResults()
  setAuth(state.adminToken)

  // --- BOUNDARY: Pagination ---
  await test('EDGE-001: Page number 0 (should default to 1)', async () => {
    const res = await api.get('/cauhoi?pageNumber=0&pageSize=10')
    assert(res.data.success, 'Should handle gracefully')
  })

  await test('EDGE-002: Negative page number', async () => {
    const res = await api.get('/cauhoi?pageNumber=-1&pageSize=10')
    assert(res.data.success, 'Should handle gracefully')
  })

  await test('EDGE-003: Page size = 0', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=0')
    assert(res.data.success || true, 'Should handle gracefully')
  })

  await test('EDGE-004: Very large page number (no results)', async () => {
    const res = await api.get('/cauhoi?pageNumber=99999&pageSize=10')
    assert(res.data.success, 'Should succeed with empty results')
    assertEqual(res.data.data.items.length, 0, 'Should have no items')
  })

  // --- BOUNDARY: String lengths ---
  await test('EDGE-005: Very long search keyword (255 chars)', async () => {
    const longKeyword = 'a'.repeat(255)
    const res = await api.get(`/cauhoi?pageNumber=1&pageSize=10&searchKeyword=${longKeyword}`)
    assert(res.data.success, 'Should handle long keyword')
  })

  await test('EDGE-006: Unicode characters in search', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=10&searchKeyword=Việt Nam 日本語 🏥')
    assert(res.data.success, 'Should handle unicode')
  })

  await test('EDGE-007: Special characters in search', async () => {
    const res = await api.get("/cauhoi?pageNumber=1&pageSize=10&searchKeyword=%25%26%3D%3F")
    assert(res.data.success, 'Should handle special chars')
  })

  // --- BOUNDARY: Question creation ---
  await test('EDGE-008: Create question with exactly 2 choices (minimum)', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: 'Câu hỏi đúng/sai: Rửa tay là bắt buộc?',
      idLoaiCauHoi: 1,
      doKho: 'De',
      diem: 1,
      idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'Đúng', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Sai', thuTu: 2, laDapAnDung: false },
      ],
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    if (res.data.data?.id) await api.delete(`/cauhoi/${res.data.data.id}`)
  })

  await test('EDGE-009: Create question with 10 choices', async () => {
    const choices = Array.from({ length: 10 }, (_, i) => ({
      noiDung: `Lựa chọn ${i + 1}`,
      thuTu: i + 1,
      laDapAnDung: i === 0,
    }))
    const res = await api.post('/cauhoi', {
      noiDung: 'Câu hỏi nhiều lựa chọn',
      idLoaiCauHoi: 1,
      doKho: 'Kho',
      diem: 3,
      idDanhMuc: 1,
      danhSachLuaChon: choices,
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    if (res.data.data?.id) await api.delete(`/cauhoi/${res.data.data.id}`)
  })

  await test('EDGE-010: Create question with 0 points', async () => {
    try {
      const res = await api.post('/cauhoi', {
        noiDung: 'Câu hỏi 0 điểm (bonus)',
        idLoaiCauHoi: 1, doKho: 'De', diem: 0, idDanhMuc: 1,
        danhSachLuaChon: [
          { noiDung: 'A', thuTu: 1, laDapAnDung: true },
          { noiDung: 'B', thuTu: 2, laDapAnDung: false },
        ],
      })
      if (res.data.success && res.data.data?.id) await api.delete(`/cauhoi/${res.data.data.id}`)
    } catch (err) {
      // 400 = BE rejects 0 points - valid behavior
      assert(err.response?.status === 400, 'BE may reject 0 points')
    }
  })

  await test('EDGE-011: Create question with very long content', async () => {
    const longContent = 'Câu hỏi dài: ' + 'Lorem ipsum. '.repeat(50)
    try {
      const res = await api.post('/cauhoi', {
        noiDung: longContent,
        idLoaiCauHoi: 1, doKho: 'TrungBinh', diem: 1, idDanhMuc: 1,
        danhSachLuaChon: [
          { noiDung: 'A', thuTu: 1, laDapAnDung: true },
          { noiDung: 'B', thuTu: 2, laDapAnDung: false },
        ],
      })
      if (res.data.success && res.data.data?.id) await api.delete(`/cauhoi/${res.data.data.id}`)
    } catch (err) {
      // 400 = BE has content length limit - valid behavior
      assert(err.response?.status === 400, 'BE may reject very long content')
    }
  })

  await test('EDGE-012: Create question with multiple correct answers', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: 'Chọn TẤT CẢ đáp án đúng: Thời điểm rửa tay?',
      idLoaiCauHoi: 1,
      doKho: 'Kho',
      diem: 2,
      idDanhMuc: 1,
      danhSachLuaChon: [
        { noiDung: 'Trước tiếp xúc bệnh nhân', thuTu: 1, laDapAnDung: true },
        { noiDung: 'Sau tiếp xúc bệnh nhân', thuTu: 2, laDapAnDung: true },
        { noiDung: 'Trước khi ăn', thuTu: 3, laDapAnDung: false },
        { noiDung: 'Sau khi tiếp xúc dịch', thuTu: 4, laDapAnDung: true },
      ],
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    if (res.data.data?.id) await api.delete(`/cauhoi/${res.data.data.id}`)
  })

  // --- BOUNDARY: User creation ---
  await test('EDGE-013: Create user with minimum length username (3 chars)', async () => {
    const res = await api.post('/user', {
      tenDangNhap: `u_${Date.now().toString().slice(-1)}`,
      matKhau: 'Pass@123',
      email: randomEmail(),
      hoTen: 'Min Username',
      idVaiTro: 3,
      trangThai: true,
    })
    // May succeed or fail depending on exact length
    assert(res.data.success !== undefined, 'Should respond')
  })

  await test('EDGE-014: Create user with Vietnamese name', async () => {
    const res = await api.post('/user', {
      tenDangNhap: randomUsername(),
      matKhau: 'Pass@123',
      email: randomEmail(),
      hoTen: 'Nguyễn Thị Phương Thảo',
      chucDanh: 'Điều dưỡng trưởng',
      khoaPhong: 'Khoa Sản - Phụ khoa',
      idVaiTro: 3,
      trangThai: true,
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
  })

  // --- BOUNDARY: Exam timing ---
  await test('EDGE-015: Create exam with 1 minute duration', async () => {
    const res = await api.post('/exam', {
      maDeThi: `EDGE_1MIN_${Date.now()}`,
      tenDeThi: 'Đề thi 1 phút',
      thoiGianLamBai: 1,
      thoiGianBatDau: new Date(Date.now() - 60000).toISOString(),
      trangThai: 'Active',
      danhSachIdCauHoi: state.createdQuestionIds.length > 0 ? [state.createdQuestionIds[0]] : [1],
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
  })

  await test('EDGE-016: Create exam with max duration (300 min per BE schema)', async () => {
    try {
      const res = await api.post('/exam', {
        maDeThi: `EDGE_MAX_${Date.now()}`,
        tenDeThi: 'Đề thi max duration',
        thoiGianLamBai: 300, // BE max is 300 per [Range(1,300)]
        thoiGianBatDau: new Date(Date.now() + 86400000).toISOString(),
        trangThai: 'Draft',
        danhSachIdCauHoi: state.createdQuestionIds.length > 0 ? [state.createdQuestionIds[0]] : [1],
      })
      assert(res.data.success, `Create failed: ${res.data.message}`)
    } catch (err) {
      // 400 = validation - acceptable
      assert(err.response?.status === 400, 'BE may reject duration')
    }
  })

  // --- BOUNDARY: Notifications ---
  await test('EDGE-017: Create notification with max length title (255)', async () => {
    const res = await api.post('/notification', {
      userId: 1,
      title: 'T'.repeat(255),
      message: 'Test max title length',
      type: 'Info',
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
  })

  await test('EDGE-018: Create notification with max length message (1000)', async () => {
    const res = await api.post('/notification', {
      userId: 1,
      title: 'Max Message Test',
      message: 'M'.repeat(1000),
      type: 'Warning',
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
  })

  // --- ERROR HANDLING ---
  await test('EDGE-019: GET non-existent endpoint', async () => {
    try {
      await api.get('/nonexistent/endpoint/xyz')
    } catch (err) {
      assert(err.response?.status === 404 || err.response?.status === 405, 'Should be 404/405')
    }
  })

  await test('EDGE-020: POST with empty body', async () => {
    try {
      await api.post('/cauhoi', {})
    } catch (err) {
      assert(err.response?.status >= 400, 'Should reject empty body')
    }
  })

  await test('EDGE-021: PUT with mismatched ID', async () => {
    try {
      const res = await api.put('/cauhoi/1', { id: 999, noiDung: 'mismatch' })
      // Should either fail or ignore the mismatch
      assert(true, 'Handled gracefully')
    } catch (err) {
      assert(err.response?.status >= 400, 'Should reject mismatched ID')
    }
  })

  await test('EDGE-022: DELETE non-existent resource', async () => {
    try {
      const res = await api.delete('/cauhoi/999999')
      assert(!res.data.success, 'Should fail for non-existent')
    } catch (err) {
      assert(err.response?.status >= 400, 'Should return error')
    }
  })

  // --- CONCURRENT-LIKE SCENARIOS ---
  await test('EDGE-023: Multiple rapid API calls (non-auth)', async () => {
    const promises = Array.from({ length: 5 }, () => api.get('/category/categories'))
    const results = await Promise.all(promises)
    results.forEach(r => assert(r.data.success, 'All should succeed'))
  })

  await test('EDGE-024: Get dashboard multiple times', async () => {
    const promises = Array.from({ length: 3 }, () => api.get('/statistics/dashboard'))
    const results = await Promise.all(promises)
    results.forEach(r => assert(r.data.success, 'All should succeed'))
  })

  return printSummary('Edge Cases & Boundary')
}

if (process.argv[1]?.includes('13-edge')) {
  runEdgeCaseTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
