import crypto from 'crypto'

// DEV-ONLY: dung dung secret trong appsettings.json cua may dev local de tu ky JWT test,
// tranh phai biet mat khau that cua tung tai khoan test. Khong dung cho moi truong that.
const SECRET = 'xxUPuDcb9+3fMmpZjS+FWLne3s159ivPW92YuNO6G5T/B76VRxpbZbX72qnxluwG'
const ISSUER = 'BanTayVang.API'
const AUDIENCE = 'BanTayVang.Client'

function b64url(input: string) {
  return Buffer.from(input).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

export interface MintTokenParams {
  userId: number
  username: string
  fullName?: string
  roleId: number
  roleName: string
  department?: string
  managedDeptId?: number
}

export function mintToken({ userId, username, fullName, roleId, roleName, department, managedDeptId }: MintTokenParams): string {
  const header = { alg: 'HS256', typ: 'JWT' }
  const now = Math.floor(Date.now() / 1000)
  const payload = {
    nameid: String(userId),
    unique_name: username,
    email: '',
    given_name: fullName || '',
    role: roleName,
    user_id: String(userId),
    username,
    full_name: fullName || '',
    role_id: String(roleId),
    is_active: 'true',
    remember_me: 'false',
    department_claim: department || '',
    managed_department_id: managedDeptId ? String(managedDeptId) : '',
    jti: crypto.randomUUID(),
    iat: now,
    nbf: now,
    exp: now + 3600 * 6,
    iss: ISSUER,
    aud: AUDIENCE,
  }
  const encHeader = b64url(JSON.stringify(header))
  const encPayload = b64url(JSON.stringify(payload))
  const signingInput = `${encHeader}.${encPayload}`
  const sig = crypto.createHmac('sha256', SECRET).update(signingInput).digest()
  const encSig = Buffer.from(sig).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `${signingInput}.${encSig}`
}
