/**
 * Test Suite 09: Notifications & Exam Schedule
 */
import {
  api, state, test, assert, assertGreaterThan,
  loginAsAdmin, printSummary, resetResults,
} from '../lib/test-helper.mjs'

export async function runNotificationTests() {
  console.log('\n🔔 ═══ 09. NOTIFICATIONS TESTS ═══')
  resetResults()
  await loginAsAdmin()

  let notifId = 0

  await test('NOTIF-001: Create notification for user', async () => {
    const res = await api.post('/notification', {
      userId: state.createdUserId || 1,
      title: 'Thông báo lịch thi',
      message: 'Bạn có lịch thi vào ngày mai lúc 8h00',
      type: 'Info',
      relatedUrl: '/exam-waiting',
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
    notifId = res.data.data.id
  })

  await test('NOTIF-002: Create warning notification', async () => {
    const res = await api.post('/notification', {
      userId: 1,
      title: 'Cảnh báo bảo mật',
      message: 'Phát hiện đăng nhập bất thường',
      type: 'Warning',
    })
    assert(res.data.success, `Create failed: ${res.data.message}`)
  })

  await test('NOTIF-003: Broadcast notification', async () => {
    const res = await api.post('/notification/broadcast', {
      title: 'Thông báo chung',
      message: 'Hệ thống sẽ bảo trì vào 22h tối nay',
      type: 'Info',
    })
    assert(res.data.success, `Broadcast failed: ${res.data.message}`)
  })

  await test('NOTIF-004: Get my notifications', async () => {
    const res = await api.get('/notification')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
    assertGreaterThan(res.data.data.length, 0, 'Should have notifications')
  })

  await test('NOTIF-005: Get unread only', async () => {
    const res = await api.get('/notification?unreadOnly=true')
    assert(res.data.success, 'Should succeed')
  })

  await test('NOTIF-006: Get unread count', async () => {
    const res = await api.get('/notification/unread-count')
    assert(res.data.success, 'Should succeed')
    assert(res.data.data >= 0, 'Count should be >= 0')
  })

  await test('NOTIF-007: Mark notification as read', async () => {
    // Get an unread notification
    const listRes = await api.get('/notification?unreadOnly=true')
    const unread = listRes.data.data?.[0]
    if (unread) {
      const res = await api.post(`/notification/${unread.id}/read`)
      assert(res.data.success, `Mark read failed: ${res.data.message}`)
    }
  })

  await test('NOTIF-008: Mark all as read', async () => {
    const res = await api.post('/notification/mark-all-read')
    assert(res.data.success, 'Should succeed')
  })

  await test('NOTIF-009: Verify all read', async () => {
    const res = await api.get('/notification/unread-count')
    assert(res.data.success, 'Should succeed')
    // After mark all read, count should be 0
    assert(res.data.data === 0, `Expected 0 unread, got ${res.data.data}`)
  })

  await test('NOTIF-010: Delete notification', async () => {
    // Create a notification for admin (userId=1) then delete it
    const createRes = await api.post('/notification', {
      userId: 1,
      title: 'To Delete',
      message: 'Will be deleted',
      type: 'Info',
    })
    if (createRes.data.success) {
      const res = await api.delete(`/notification/${createRes.data.data.id}`)
      assert(res.data.success, `Delete failed: ${res.data.message}`)
    }
  })

  // --- EXAM SCHEDULE ---
  await test('NOTIF-011: Get upcoming exams', async () => {
    const res = await api.get('/notification/upcoming-exams')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  await test('NOTIF-012: Get current exams', async () => {
    const res = await api.get('/notification/current-exams')
    assert(res.data.success, 'Should succeed')
    assert(Array.isArray(res.data.data), 'Should return array')
  })

  return printSummary('Notifications')
}

if (process.argv[1]?.includes('09-notifications')) {
  runNotificationTests().then(({ failed }) => process.exit(failed > 0 ? 1 : 0))
}
