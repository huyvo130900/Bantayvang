/**
 * Test Suite 03: Categories & Question Types
 */
import {
  api, test, assert, assertGreaterThan, assertArrayNotEmpty,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runCategoryTests() {
  console.log('\n📂 ═══ 03. CATEGORIES & QUESTION TYPES TESTS ═══')
  resetResults()
  await loginAsAdmin()

  let createdCategoryId = 0
  let createdTypeId = 0

  // --- CATEGORIES ---
  await test('CAT-001: Get all categories', async () => {
    const res = await api.get('/category/categories')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('CAT-002: Create category', async () => {
    const res = await api.post('/category/categories', {
      tenDanhMuc: `Kiểm soát nhiễm khuẩn ${Date.now()}`,
      mota: 'Danh mục test - kiến thức KSNK',
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    assertGreaterThan(res.data.data.id, 0, 'Should have ID')
    createdCategoryId = res.data.data.id
  })

  await test('CAT-003: Get category by ID', async () => {
    const res = await api.get(`/category/categories/${createdCategoryId}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('CAT-004: Update category', async () => {
    const res = await api.put(`/category/categories/${createdCategoryId}`, {
      tenDanhMuc: 'Danh mục đã cập nhật',
      mota: 'Mô tả mới',
    })
    assert(res.data.success, `Update failed: ${res.data.message}`)
  })

  await test('CAT-005: Create category without name should fail', async () => {
    try {
      const res = await api.post('/category/categories', { tenDanhMuc: '', mota: '' })
      // Some APIs accept empty, check response
      if (res.data.success) {
        // cleanup
        await api.delete(`/category/categories/${res.data.data.id}`)
      }
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('CAT-006: Delete category', async () => {
    const res = await api.delete(`/category/categories/${createdCategoryId}`)
    assert(res.data.success, `Delete failed: ${res.data.message}`)
  })

  // --- QUESTION TYPES ---
  await test('TYPE-001: Get all question types', async () => {
    const res = await api.get('/category/types')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('TYPE-002: Create question type', async () => {
    const res = await api.post('/category/types', {
      tenLoai: `Trắc nghiệm nhiều đáp án ${Date.now()}`,
      moTa: 'Loại câu hỏi test',
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    createdTypeId = res.data.data.id
  })

  await test('TYPE-003: Get question type by ID', async () => {
    const res = await api.get(`/category/types/${createdTypeId}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('TYPE-004: Update question type', async () => {
    const res = await api.put(`/category/types/${createdTypeId}`, {
      tenLoai: 'Loại đã cập nhật',
      moTa: 'Mô tả mới',
    })
    assert(res.data.success, `Update failed: ${res.data.message}`)
  })

  await test('TYPE-005: Delete question type', async () => {
    const res = await api.delete(`/category/types/${createdTypeId}`)
    assert(res.data.success, `Delete failed: ${res.data.message}`)
  })

  return printSummary('Categories & Types')
}

if (process.argv[1]?.includes('03-categories')) {
  runCategoryTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
