// Direct backend calls used for test setup/teardown (seeding + cleaning up test data) and for
// admin bootstrap. Keeps spec files focused on UI behaviour instead of plumbing.
const API_BASE = 'http://localhost:5293/api'

export async function apiGet(path: string, token?: string) {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  return { status: res.status, body: await safeJson(res) }
}

export async function apiPost(path: string, token: string | undefined, data: unknown) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(data),
  })
  return { status: res.status, body: await safeJson(res) }
}

export async function apiPut(path: string, token: string | undefined, data: unknown) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(data),
  })
  return { status: res.status, body: await safeJson(res) }
}

async function safeJson(res: Response) {
  const text = await res.text()
  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

export async function ensureAdminPassword(adminToken: string) {
  return apiPost('/Seed/reset-admin-password', adminToken, {})
}
