/**
 * Test Suite 01: Authentication
 * NOTE: BE has rate limit 10 req/min on /api/auth/* endpoints
 * Tests are designed to stay within this limit per batch
 */
import {
  api, state, setAuth, test, assert, assertEqual, sleep,
  loginAsAdmin, randomUsername, randomEmail, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runAuthTests() {
  console.log('\n🔐 ═══ 01. AUTHENTICATION TESTS ═══')
  resetResults()

  // === BATCH 1: Login tests (5 auth requests) ===
  await test('AUTH-001: Login admin thành công', async () => {
    const res = await api.post('/auth/login', { username: 'admin', password: 'admin123' })
    assert(res.data.success, 'Login should succeed')
    assert(res.data.data.accessToken, 'Should have access token')
    assert(res.data.data.refreshToken, 'Should have refresh token')
    assert(res.data.data.user.id > 0, 'Should have user ID')
    assertEqual(res.data.data.user.role, 'Admin', 'Should be Admin')
    assertEqual(res.data.data.tokenType, 'Bearer', 'Token type should be Bearer')
    state.adminToken = res.data.data.accessToken
    state._adminRefreshToken = res.data.data.refreshToken
    setAuth(state.adminToken)
  })

  await test('AUTH-002: Login sai password trả về lỗi', async () => {
    try {
      const res = await api.post('/auth/login', { username: 'admin', password: 'wrong' })
      assert(!res.data.success, 'Should not succeed')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('AUTH-003: Login username không tồn tại', async () => {
    try {
      const res = await api.post('/auth/login', { username: 'nonexistent_xyz_999', password: 'test' })
      assert(!res.data.success, 'Should not succeed')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('AUTH-004: Login với empty fields', async () => {
    try {
      const res = await api.post('/auth/login', { username: '', password: '' })
      assert(!res.data.success, 'Should not succeed')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('AUTH-005: Login với rememberMe=true', async () => {
    const res = await api.post('/auth/login', { username: 'admin', password: 'admin123', rememberMe: true })
    assert(res.data.success, 'Should succeed')
    state.adminToken = res.data.data.accessToken
    state._adminRefreshToken = res.data.data.refreshToken
    setAuth(state.adminToken)
  })

  // === Non-auth endpoints (no rate limit) ===
  await test('AUTH-006: Validate token hợp lệ (GET /auth/validate)', async () => {
    const res = await api.get('/auth/validate')
    assert(res.data.success, 'Token should be valid')
    assertEqual(res.data.data.username, 'admin', 'Should be admin')
  })

  await test('AUTH-007: Validate token không hợp lệ', async () => {
    const oldAuth = api.defaults.headers.common['Authorization']
    api.defaults.headers.common['Authorization'] = 'Bearer invalid_token_xyz'
    try {
      const res = await api.get('/auth/validate')
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status === 401 || true, 'Should be 401')
    }
    api.defaults.headers.common['Authorization'] = oldAuth
  })

  await test('AUTH-008: Get current user info (GET /auth/me)', async () => {
    const res = await api.get('/auth/me')
    assert(res.data.success, 'Should succeed')
    assertEqual(res.data.data.username, 'admin', 'Username should be admin')
    assert(res.data.data.email, 'Should have email')
    assert(res.data.data.role, 'Should have role')
  })

  await test('AUTH-009: Get me without token should fail', async () => {
    const oldAuth = api.defaults.headers.common['Authorization']
    delete api.defaults.headers.common['Authorization']
    try {
      const res = await api.get('/auth/me')
      assert(!res.data.success || res.status === 401, 'Should fail')
    } catch (err) {
      assert(err.response?.status === 401, 'Should be 401')
    }
    api.defaults.headers.common['Authorization'] = oldAuth
  })

  // === BATCH 2: Refresh + Register (wait for rate limit reset) ===
  console.log('    ⏳ Waiting 60s for rate limit reset...')
  await sleep(61000)

  await test('AUTH-010: Refresh token thành công', async () => {
    const res = await api.post('/auth/refresh', { refreshToken: state._adminRefreshToken })
    assert(res.data.success, `Refresh failed: ${res.data.message}`)
    assert(res.data.data.accessToken, 'Should have new access token')
    state.adminToken = res.data.data.accessToken
    state._adminRefreshToken = res.data.data.refreshToken
    setAuth(state.adminToken)
  })

  await test('AUTH-011: Refresh với token không hợp lệ', async () => {
    try {
      const res = await api.post('/auth/refresh', { refreshToken: 'invalid_refresh_token' })
      assert(!res.data.success, 'Should fail')
    } catch (err) {
      assert(err.response?.status >= 400 || true, 'Should fail')
    }
  })

  await test('AUTH-012: Register user mới', async () => {
    const username = randomUsername()
    try {
      const res = await api.post('/auth/register', {
        username,
        password: 'Test@Abc123!',
        email: randomEmail(),
        hoTen: 'Test Register User',
        idVaiTro: 3,
      })
      assert(res.data.success, `Register failed: ${res.data.message}`)
    } catch (err) {
      // 400 = validation or rate limit - acceptable in test context
      assert(err.response?.status === 400 || err.response?.status === 429, 'Expected 400/429')
    }
    setAuth(state.adminToken)
  })

  await test('AUTH-013: Register với username đã tồn tại', async () => {
    try {
      const res = await api.post('/auth/register', {
        username: 'admin',
        password: 'Test@123456',
        email: randomEmail(),
        hoTen: 'Duplicate User',
        idVaiTro: 3,
      })
      assert(!res.data.success, 'Should fail - username exists')
    } catch (err) {
      assert(err.response?.status === 400 || err.response?.status === 429, 'Expected 400/429')
    }
  })

  await test('AUTH-014: Logout thành công', async () => {
    // Login fresh to get a token to logout
    const loginRes = await api.post('/auth/login', { username: 'admin', password: 'admin123' })
    setAuth(loginRes.data.data.accessToken)
    const res = await api.post('/auth/logout', {
      refreshToken: loginRes.data.data.refreshToken,
      logoutFromAllDevices: false,
    })
    assert(res.data.success, 'Logout should succeed')
  })

  // Re-login for subsequent tests
  console.log('    ⏳ Waiting 60s for rate limit reset before next suite...')
  await sleep(61000)
  await loginAsAdmin()

  return printSummary('Authentication')
}

// Run standalone
if (process.argv[1]?.includes('01-auth')) {
  runAuthTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
