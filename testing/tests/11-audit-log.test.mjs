/**
 * Test Suite 11: Audit Log
 */
import {
  api, state, setAuth, test, assert, assertArrayNotEmpty,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runAuditLogTests() {
  console.log('\n📋 ═══ 11. AUDIT LOG TESTS ═══')
  resetResults()
  // Use existing admin token, don't login again
  setAuth(state.adminToken)

  await test('AUDIT-001: Get recent logs', async () => {
    const res = await api.get('/auditlog/recent?top=50')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('AUDIT-002: Get recent logs with custom top', async () => {
    const res = await api.get('/auditlog/recent?top=10')
    assert(res.data.success, 'Should succeed')
    assert(res.data.data.length <= 10, 'Should respect top limit')
  })

  await test('AUDIT-003: Get logs by user (admin)', async () => {
    const res = await api.get('/auditlog/user/1?top=20')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('AUDIT-004: Get logs by created user', async () => {
    if (state.createdUserId) {
      const res = await api.get(`/auditlog/user/${state.createdUserId}?top=10`)
      assert(res.data.success, 'Should succeed')
    }
  })

  await test('AUDIT-005: Get logs by exam session', async () => {
    if (state.createdBaithiId) {
      const res = await api.get(`/auditlog/exam-session/${state.createdBaithiId}`)
      assert(res.data.success, 'Should succeed')
      assert(Array.isArray(res.data.data), 'Should return array')
    }
  })

  await test('AUDIT-006: Search logs by action type', async () => {
    const res = await api.get('/auditlog/search?actionType=POST')
    assert(res.data.success, 'Should succeed')
  })

  await test('AUDIT-007: Search logs by date range', async () => {
    const from = new Date(Date.now() - 86400000).toISOString()
    const to = new Date().toISOString()
    const res = await api.get(`/auditlog/search?from=${from}&to=${to}`)
    assert(res.data.success, 'Should succeed')
  })

  await test('AUDIT-008: Search with all filters', async () => {
    const from = new Date(Date.now() - 7 * 86400000).toISOString()
    const to = new Date().toISOString()
    const res = await api.get(`/auditlog/search?actionType=GET&from=${from}&to=${to}`)
    assert(res.data.success, 'Should succeed')
  })

  return printSummary('Audit Log')
}

if (process.argv[1]?.includes('11-audit')) {
  runAuditLogTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
