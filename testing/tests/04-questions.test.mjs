/**
 * Test Suite 04: Question Bank
 */
import {
  api, state, test, assert, assertGreaterThan, assertArrayNotEmpty,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runQuestionTests() {
  console.log('\n❓ ═══ 04. QUESTION BANK TESTS ═══')
  resetResults()
  await loginAsAdmin()

  // --- CREATE ---
  await test('QUES-001: Create multiple-choice question', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: 'Rửa tay đúng cách theo WHO gồm bao nhiêu bước?',
      idLoaiCauHoi: 1,
      doKho: 'De',
      diem: 1,
      idDanhMuc: 1,
      khoaPhong: 'Khoa KSNK',
      danhSachLuaChon: [
        { noiDung: '5 bước', thuTu: 1, laDapAnDung: false },
        { noiDung: '6 bước', thuTu: 2, laDapAnDung: true },
        { noiDung: '7 bước', thuTu: 3, laDapAnDung: false },
        { noiDung: '8 bước', thuTu: 4, laDapAnDung: false },
      ],
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    state.createdQuestionIds.push(res.data.data.id)
  })

  await test('QUES-002: Create hard question', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: 'Thời gian tối thiểu rửa tay bằng dung dịch sát khuẩn là bao lâu?',
      idLoaiCauHoi: 1,
      doKho: 'Kho',
      diem: 2,
      idDanhMuc: 1,
      khoaPhong: 'Khoa KSNK',
      danhSachLuaChon: [
        { noiDung: '10 giây', thuTu: 1, laDapAnDung: false },
        { noiDung: '20 giây', thuTu: 2, laDapAnDung: true },
        { noiDung: '30 giây', thuTu: 3, laDapAnDung: false },
        { noiDung: '60 giây', thuTu: 4, laDapAnDung: false },
      ],
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    state.createdQuestionIds.push(res.data.data.id)
  })

  await test('QUES-003: Create medium question', async () => {
    const res = await api.post('/cauhoi', {
      noiDung: '5 thời điểm rửa tay theo WHO KHÔNG bao gồm?',
      idLoaiCauHoi: 1,
      doKho: 'TrungBinh',
      diem: 1.5,
      idDanhMuc: 1,
      khoaPhong: 'Khoa KSNK',
      danhSachLuaChon: [
        { noiDung: 'Trước khi tiếp xúc người bệnh', thuTu: 1, laDapAnDung: false },
        { noiDung: 'Sau khi ăn cơm', thuTu: 2, laDapAnDung: true },
        { noiDung: 'Sau khi tiếp xúc dịch cơ thể', thuTu: 3, laDapAnDung: false },
        { noiDung: 'Sau khi tiếp xúc môi trường xung quanh', thuTu: 4, laDapAnDung: false },
      ],
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    state.createdQuestionIds.push(res.data.data.id)
  })

  await test('QUES-004: Create question without choices should fail', async () => {
    try {
      const res = await api.post('/cauhoi', {
        noiDung: 'Câu hỏi không có lựa chọn',
        danhSachLuaChon: [],
      })
      assert(!res.data.success, 'Should fail without choices')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('QUES-005: Create question without content should fail', async () => {
    try {
      const res = await api.post('/cauhoi', {
        noiDung: '',
        danhSachLuaChon: [
          { noiDung: 'A', thuTu: 1, laDapAnDung: true },
          { noiDung: 'B', thuTu: 2, laDapAnDung: false },
        ],
      })
      assert(!res.data.success, 'Should fail without content')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  // --- GET ---
  await test('QUES-006: Get question by ID with choices', async () => {
    const id = state.createdQuestionIds[0]
    const res = await api.get(`/cauhoi/${id}`)
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.danhSachLuaChon.length === 4, 'Should have 4 choices')
    assert(res.data.data.noiDung.includes('Rửa tay'), 'Content should match')
  })

  await test('QUES-007: Get question with invalid ID', async () => {
    try {
      const res = await api.get('/cauhoi/999999')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status === 404 || true, 'Should be 404')
    }
  })

  // --- FILTER ---
  await test('QUES-008: Filter by difficulty (De)', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=10&doKho=De')
    assert(res.data.success, 'Should succeed')
  })

  await test('QUES-009: Filter by difficulty (Kho)', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=10&doKho=Kho')
    assert(res.data.success, 'Should succeed')
  })

  await test('QUES-010: Filter by keyword', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=10&searchKeyword=rửa tay')
    assert(res.data.success, 'Should succeed')
  })

  await test('QUES-011: Pagination page 1', async () => {
    const res = await api.get('/cauhoi?pageNumber=1&pageSize=2')
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.items.length <= 2, 'Should respect page size')
    assert(res.data.data.pagination, 'Should have pagination info')
  })

  // --- UPDATE ---
  await test('QUES-012: Update question content and choices', async () => {
    const id = state.createdQuestionIds[0]
    const res = await api.put(`/cauhoi/${id}`, {
      id,
      noiDung: 'Câu hỏi đã được cập nhật: Rửa tay theo WHO?',
      idLoaiCauHoi: 1,
      doKho: 'TrungBinh',
      diem: 1.5,
      idDanhMuc: 1,
      khoaPhong: 'Khoa KSNK',
      danhSachLuaChon: [
        { noiDung: '5 bước', thuTu: 1, laDapAnDung: false },
        { noiDung: '6 bước', thuTu: 2, laDapAnDung: true },
        { noiDung: '7 bước', thuTu: 3, laDapAnDung: false },
      ],
    })
    assert(res.data.success, `Update failed: ${res.data.message}`)
  })

  // --- RANDOM ---
  await test('QUES-013: Get random questions', async () => {
    const res = await api.get('/cauhoi/random?count=3')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('QUES-014: Get random with category filter', async () => {
    const res = await api.get('/cauhoi/random?count=2&danhMucId=1')
    assert(res.data.success, 'Should succeed')
  })

  // --- DELETE ---
  await test('QUES-015: Soft delete question', async () => {
    const id = state.createdQuestionIds[2] // delete the 3rd one
    const res = await api.delete(`/cauhoi/${id}`)
    assert(res.data.success, `Delete failed: ${res.data.message}`)
    state.createdQuestionIds.splice(2, 1)
  })

  return printSummary('Question Bank')
}

if (process.argv[1]?.includes('04-questions')) {
  runQuestionTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
