import { api, setAuth } from './lib/test-helper.mjs'

async function run() {
  try {
    console.log('Logging in as manager...')
    const loginRes = await api.post('/auth/login', { username: 'manager', password: 'admin123' })
    console.log('Login response:', JSON.stringify(loginRes.data, null, 2))
    
    const token = loginRes.data.data.accessToken
    setAuth(token)

    console.log('\nFetching questions...')
    const questionsRes = await api.get('/cauhoi')
    console.log('Questions response status:', questionsRes.status)
    console.log('Questions response data:', JSON.stringify(questionsRes.data, null, 2))
  } catch (err) {
    console.error('Error occurred:', err.response?.data || err.message)
  }
}

run()
