import {
  api, test, assert, loginAsAdmin, resetResults, printSummary
} from '../lib/test-helper.mjs'

export async function runUserExcelImportTests() {
  console.log('\n📊 ═══ 27. USER EXCEL IMPORT TESTS ═══')
  resetResults()
  await loginAsAdmin()

  let templateBuffer = null

  await test('IMPORT-001: Download user import template', async () => {
    const res = await api.get('/user/import-template', { responseType: 'arraybuffer' })
    assert(res.status === 200, 'Should return HTTP 200')
    assert(res.data.byteLength > 0, 'Template file should not be empty')
    assertEqual(res.headers['content-type'], 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'Should have correct excel mime-type')
    templateBuffer = res.data
  })

  await test('IMPORT-002: Import users from downloaded template', async () => {
    assert(templateBuffer !== null, 'Template must be downloaded first')

    // Construct FormData using Node's global FormData and Blob
    const blob = new Blob([templateBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const formData = new FormData()
    formData.append('file', blob, 'MauImportTaiKhoan.xlsx')

    const res = await api.post('/user/import', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })

    assert(res.status === 200, 'Should return HTTP 200')
    assert(res.data.success, `Import should succeed: ${res.data.message}`)
    
    const importResult = res.data.data
    console.log(`   -> Success count: ${importResult.success}, Failed count: ${importResult.failed}`)
    
    // The template has 3 pre-populated sample users: NV001, NV002, NV003
    // Depending on whether these users already exist in the DB from previous runs:
    // Success + Failed should equal 3.
    const totalProcessed = importResult.success + importResult.failed
    assertEqual(totalProcessed, 3, 'Should process exactly 3 sample rows')
  })

  return printSummary()
}

// Simple assertion helper since it might not be exported from test-helper.mjs
function assertEqual(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(`${message}: expected ${expected}, got ${actual}`)
  }
}

if (process.argv[1]?.includes('27-user-excel-import')) {
  runUserExcelImportTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
