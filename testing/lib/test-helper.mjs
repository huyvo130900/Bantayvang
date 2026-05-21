/**
 * Test helper utilities for BanTayVang API tests
 */
import https from 'https'
import axios from 'axios'

const BASE_URL = 'https://localhost:7249/api'

// Shared axios instance (skip SSL for localhost)
export const api = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  httpsAgent: new https.Agent({ rejectUnauthorized: false }),
  timeout: 15000,
})

// Test state shared across test files
export const state = {
  adminToken: '',
  studentToken: '',
  createdUserId: 0,
  createdUsername: '',
  createdQuestionIds: [],
  createdExamCode: '',
  createdExamId: 0,
  createdBaithiId: 0,
  createdKyThiId: 0,
}

// Auth helper
export function setAuth(token) {
  api.defaults.headers.common['Authorization'] = `Bearer ${token}`
}

// Test runner
let _passed = 0
let _failed = 0
const _results = []

export async function test(name, fn) {
  try {
    await fn()
    _passed++
    _results.push({ name, status: 'PASS' })
    console.log(`  ✅ ${name}`)
  } catch (error) {
    _failed++
    const msg = error.response?.data?.message || error.message || String(error)
    _results.push({ name, status: 'FAIL', error: msg })
    console.log(`  ❌ ${name} → ${msg}`)
  }
}

export function assert(condition, message) {
  if (!condition) throw new Error(message || 'Assertion failed')
}

export function assertEqual(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(message || `Expected "${expected}" but got "${actual}"`)
  }
}

export function assertContains(str, substring, message) {
  if (!str || !str.includes(substring)) {
    throw new Error(message || `Expected "${str}" to contain "${substring}"`)
  }
}

export function assertGreaterThan(actual, expected, message) {
  if (actual <= expected) {
    throw new Error(message || `Expected ${actual} > ${expected}`)
  }
}

export function assertArrayNotEmpty(arr, message) {
  if (!Array.isArray(arr) || arr.length === 0) {
    throw new Error(message || 'Expected non-empty array')
  }
}

export const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms))

export function getResults() {
  return { passed: _passed, failed: _failed, results: _results }
}

export function resetResults() {
  _passed = 0
  _failed = 0
  _results.length = 0
}

export function printSummary(suiteName) {
  console.log(`\n  📊 ${suiteName}: ${_passed} passed, ${_failed} failed`)
  if (_failed > 0) {
    _results.filter(r => r.status === 'FAIL').forEach(r => {
      console.log(`     ↳ ${r.name}: ${r.error}`)
    })
  }
  return { passed: _passed, failed: _failed }
}

// Login helper
export async function loginAsAdmin() {
  // Reuse existing token if available
  if (state.adminToken) {
    setAuth(state.adminToken)
    return { success: true }
  }
  const res = await api.post('/auth/login', { username: 'admin', password: 'admin123' })
  if (res.data.success) {
    state.adminToken = res.data.data.accessToken
    setAuth(state.adminToken)
  }
  return res.data
}

export async function loginAsStudent(username, password) {
  const res = await api.post('/auth/login', { username, password })
  if (res.data.success) {
    state.studentToken = res.data.data.accessToken
  }
  return res.data
}

// Random data generators
export function randomString(length = 8) {
  return Math.random().toString(36).substring(2, 2 + length)
}

export function randomEmail() {
  return `test_${Date.now()}_${randomString(4)}@test.com`
}

export function randomUsername() {
  return `user_${Date.now()}_${randomString(4)}`
}

export function randomExamCode() {
  return `EXAM_${Date.now()}`
}
