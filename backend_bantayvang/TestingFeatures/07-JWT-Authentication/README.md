# JWT Authentication Testing Guide

## 🎯 Overview
This directory contains comprehensive tests for the JWT Authentication system, covering security, functionality, and OWASP compliance.

## 📁 Test Files

### 1. `01-login-test.http`
**Purpose:** Test login functionality and security
**Tests:**
- ✅ Valid login credentials
- ❌ Invalid credentials
- ❌ Empty credentials  
- 🔒 SQL injection attempts
- 🔒 XSS attempts
- ⏰ Remember me functionality

### 2. `02-token-operations.http`
**Purpose:** Test JWT token lifecycle
**Tests:**
- ✅ Token validation
- ✅ Protected endpoint access
- 🔄 Token refresh
- ❌ Invalid token handling
- ❌ Expired token handling
- 🚪 Logout operations

### 3. `03-password-operations.http`
**Purpose:** Test password management
**Tests:**
- 🔑 Password change
- 🔒 Password strength validation
- 📧 Password reset request
- 🔄 Password reset with token
- ❌ Invalid password operations

### 4. `04-role-based-access.http`
**Purpose:** Test authorization and roles
**Tests:**
- 👑 Admin-only endpoints
- 🚫 Unauthorized access attempts
- 🎭 Role-based permissions
- 🔒 Token tampering detection

## 🚀 How to Run Tests

### Prerequisites
1. ✅ Database migration completed
2. ✅ API server running (`dotnet run`)
3. ✅ Default admin user exists (username: `admin`, password: `admin123`)

### Step-by-Step Testing

#### Phase 1: Basic Authentication
1. **Start API server**
   ```bash
   cd BanTayVang.API
   dotnet run
   ```

2. **Test Login** (`01-login-test.http`)
   - Run test #1 (Valid login)
   - Copy `accessToken` and `refreshToken` from response
   - Run security tests (#4, #5) to verify input validation

#### Phase 2: Token Operations
3. **Update Variables** (`02-token-operations.http`)
   - Replace `@accessToken` with token from login
   - Replace `@refreshToken` with refresh token from login

4. **Test Protected Endpoints**
   - Run test #1 (Token validation)
   - Run test #2 (Get current user)
   - Verify unauthorized access fails (#4, #5, #6)

#### Phase 3: Password Management
5. **Test Password Operations** (`03-password-operations.http`)
   - Update `@accessToken` variable
   - Test password change (be careful - will change admin password!)
   - Test password reset flow

#### Phase 4: Role-Based Access
6. **Test Authorization** (`04-role-based-access.http`)
   - Test admin-only endpoints
   - Verify role restrictions work
   - Test token tampering detection

## 📊 Expected Results

### ✅ Success Scenarios
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... }
}
```

### ❌ Error Scenarios
```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error messages"]
}
```

### 🔒 Security Test Results
- **SQL Injection:** Should return validation error, not database error
- **XSS Attempts:** Should sanitize input and return validation error
- **Token Tampering:** Should return 401 Unauthorized
- **Invalid Tokens:** Should return 401 Unauthorized

## 🔍 Monitoring & Logging

### Security Events to Check
After running tests, check database for security events:

```sql
-- Check security events (if SecurityEvents table exists)
SELECT TOP 10 * FROM SecurityEvents 
ORDER BY Timestamp DESC;

-- Check user sessions
SELECT * FROM UserSessions 
WHERE UserId = 1 -- Admin user
ORDER BY CreatedAt DESC;

-- Check refresh tokens
SELECT * FROM RefreshTokens 
WHERE UserId = 1 -- Admin user
ORDER BY CreatedAt DESC;
```

### Application Logs
Monitor console output for:
- ✅ Successful authentication events
- ❌ Failed login attempts
- 🔒 Security violations
- 🔄 Token refresh operations

## 🛡️ Security Validation Checklist

### OWASP A01: Broken Access Control
- [ ] Unauthorized access blocked
- [ ] Role-based restrictions enforced
- [ ] User context properly validated

### OWASP A02: Cryptographic Failures
- [ ] Passwords properly hashed (BCrypt)
- [ ] JWT tokens properly signed
- [ ] Sensitive data not exposed in logs

### OWASP A03: Injection
- [ ] SQL injection attempts blocked
- [ ] XSS attempts sanitized
- [ ] Input validation working

### OWASP A07: Authentication Failures
- [ ] Strong password requirements
- [ ] Account lockout (if implemented)
- [ ] Session management secure

### OWASP A09: Security Logging
- [ ] Authentication events logged
- [ ] Failed attempts logged
- [ ] Security violations logged

## 🚨 Troubleshooting

### Common Issues

#### 1. "Connection refused" Error
- ✅ Ensure API server is running
- ✅ Check port number (default: 7001)
- ✅ Verify HTTPS certificate

#### 2. "Invalid token" Error
- ✅ Check token expiration
- ✅ Verify token format (Bearer prefix)
- ✅ Ensure token not tampered

#### 3. Database Connection Error
- ✅ Verify connection string
- ✅ Ensure SQL Server running
- ✅ Check database exists

#### 4. Migration Issues
- ✅ Run migration script again
- ✅ Check database permissions
- ✅ Verify table creation

### Debug Commands
```bash
# Check API server status
curl -k https://localhost:7001/health

# Verify database connection
sqlcmd -S localhost -E -Q "SELECT COUNT(*) FROM HeThongBanTayVang.dbo.TAIKHOAN"

# Check JWT configuration
dotnet run --environment Development
```

## 📈 Performance Testing

### Load Testing (Optional)
```bash
# Install artillery (if needed)
npm install -g artillery

# Create load test config
# artillery quick --count 10 --num 5 https://localhost:7001/api/auth/login
```

## 🎯 Next Steps After Testing

1. **If tests pass:** Proceed to update existing controllers with authentication
2. **If tests fail:** Debug and fix issues before proceeding
3. **Security review:** Ensure all OWASP requirements met
4. **Performance review:** Check response times and resource usage

---

**🔐 Security Note:** Always change default admin password after testing!
**📝 Documentation:** Update this README with any new test scenarios or findings.