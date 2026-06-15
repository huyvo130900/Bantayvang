# 🧪 Complete Testing Guide - BanTayVang API

## 📋 Tổng quan
Hướng dẫn chi tiết để test toàn bộ hệ thống BanTayVang API từ cơ bản đến nâng cao.

## 🎯 Mục tiêu Testing
- ✅ Verify tất cả tính năng hoạt động đúng
- ✅ Test performance và scalability  
- ✅ Kiểm tra data consistency
- ✅ Validate error handling
- ✅ Test security và anti-cheat

## 🚀 Quy trình Testing (Thực hiện theo thứ tự)

### Phase 1: Database Setup & Preparation
**Folder: `01-Database-Setup/`**

1. **Chạy SQL Scripts theo thứ tự:**
   ```sql
   -- Chạy lần lượt:
   TestData/01-SampleData-Categories-Types.sql
   TestData/02-SampleData-Questions.sql  
   TestData/03-SampleData-Exams.sql
   ```

2. **Verify Database:**
   ```sql
   -- Chạy file verify:
   TestingFeatures/01-Database-Setup/verify-data.sql
   ```

3. **Expected Results:**
   - Categories: 5 records
   - Question Types: 4 records  
   - Users: 3 records
   - Questions: 15+ records
   - Exams: 3+ records

### Phase 2: Question Management Testing
**Folder: `02-Question-Management/`**

1. **Basic CRUD Operations:**
   - File: `test-crud.http`
   - Test: Create, Read, Update, Delete questions
   - Verify: All operations return correct status codes

2. **Search & Filter Testing:**
   - File: `test-search-filter.http`
   - Test: Search by keyword, filter by category/difficulty
   - Verify: Results match search criteria

3. **Validation Testing:**
   - File: `test-validation.http`
   - Test: Invalid data, missing fields, constraints
   - Verify: Proper error messages returned

### Phase 3: Exam System Testing
**Folder: `03-Exam-System/`**

1. **Complete Exam Flow:**
   - File: `complete-exam-flow.http`
   - Test: Create exam → Start session → Answer questions → Submit
   - Verify: Full workflow works end-to-end

2. **Session Management:**
   - File: `session-management.http`
   - Test: Multiple sessions, session states, timeouts
   - Verify: Sessions are isolated and managed correctly

3. **Grading System:**
   - File: `grading-system.http`
   - Test: Auto-grading, score calculation, results
   - Verify: Scores are calculated correctly

### Phase 4: Anti-Cheat Testing
**Folder: `04-Anti-Cheat/`**

1. **Behavior Detection:**
   - File: `behavior-detection.http`
   - Test: Tab switching, copy-paste, suspicious activities
   - Verify: Events are logged correctly

2. **Warning System:**
   - File: `warning-system.http`
   - Test: Warning thresholds, escalation, auto-submit
   - Verify: Warnings trigger appropriate actions

3. **Monitoring Dashboard:**
   - File: `monitoring-dashboard.http`
   - Test: Real-time monitoring, alerts, reports
   - Verify: Dashboard shows accurate data

### Phase 5: API Integration Testing
**Folder: `05-API-Testing/`**

1. **Authentication & Authorization:**
   - File: `auth-testing.http`
   - Test: Login, JWT tokens, role-based access
   - Verify: Security controls work properly

2. **Error Handling:**
   - File: `error-handling.http`
   - Test: Invalid requests, server errors, edge cases
   - Verify: Appropriate error responses

3. **Rate Limiting:**
   - File: `rate-limiting.http`
   - Test: Request limits, throttling, abuse prevention
   - Verify: Rate limits are enforced

### Phase 6: Integration & Performance Testing
**Folder: `06-Integration-Tests/`**

1. **Complete Lifecycle:**
   - File: `complete-lifecycle.http`
   - Test: Full question-to-result workflow
   - Verify: All components work together

2. **Multi-User Scenarios:**
   - File: `multi-user-scenario.http`
   - Test: Concurrent users, same exam, isolation
   - Verify: No data corruption or conflicts

3. **Data Consistency:**
   - File: `data-consistency.http`
   - Test: ACID properties, foreign keys, cascades
   - Verify: Data integrity maintained

4. **Performance Testing:**
   - File: `performance-integration.http`
   - Test: Load testing, response times, memory usage
   - Verify: Performance meets requirements

## 📊 Success Criteria

### ✅ Functional Requirements
- [ ] All CRUD operations work correctly
- [ ] Exam flow completes successfully
- [ ] Anti-cheat detection functions
- [ ] Grading system calculates scores accurately
- [ ] User authentication and authorization work
- [ ] Error handling provides meaningful messages

### ⚡ Performance Requirements
- [ ] API response times < 500ms for most operations
- [ ] Database queries optimized
- [ ] Memory usage remains stable
- [ ] System handles concurrent users
- [ ] No memory leaks during extended testing

### 🔒 Security Requirements
- [ ] Authentication required for protected endpoints
- [ ] Role-based access control enforced
- [ ] Input validation prevents injection attacks
- [ ] Anti-cheat system detects violations
- [ ] Sensitive data is protected

### 📈 Data Integrity Requirements
- [ ] Foreign key constraints enforced
- [ ] Transactions maintain ACID properties
- [ ] Cascade operations work correctly
- [ ] No orphaned records after deletions
- [ ] Concurrent operations don't corrupt data

## 🛠️ Tools Required

### API Testing
- **REST Client:** VS Code REST Client extension hoặc Postman
- **Authentication:** JWT tokens for different user roles
- **Environment:** Development server running on localhost:7001

### Database Testing
- **SQL Client:** SQL Server Management Studio hoặc Azure Data Studio
- **Connection:** HeThongBanTayVang database
- **Permissions:** Read/Write access for testing

### Performance Monitoring
- **Response Times:** Monitor API response times
- **Memory Usage:** Track server memory consumption
- **Database Performance:** Monitor query execution times
- **Concurrent Load:** Test with multiple simultaneous users

## 🚨 Common Issues & Solutions

### Database Connection Issues
```
Problem: Cannot connect to database
Solution: Check connection string in appsettings.json
```

### Authentication Failures
```
Problem: 401 Unauthorized errors
Solution: Ensure JWT token is valid and included in requests
```

### Foreign Key Violations
```
Problem: FK constraint errors in SQL scripts
Solution: Run scripts in correct order, verify referenced IDs exist
```

### Performance Issues
```
Problem: Slow API responses
Solution: Check database indexes, optimize queries, monitor memory
```

## 📝 Test Reporting

### Daily Testing Checklist
- [ ] All API endpoints responding
- [ ] Database connections stable
- [ ] No error logs in application
- [ ] Performance within acceptable limits
- [ ] Security controls functioning

### Weekly Integration Testing
- [ ] Full end-to-end workflows tested
- [ ] Multi-user scenarios validated
- [ ] Performance benchmarks met
- [ ] Data consistency verified
- [ ] Error handling comprehensive

### Release Testing
- [ ] All test suites pass
- [ ] Performance requirements met
- [ ] Security audit completed
- [ ] Documentation updated
- [ ] Deployment procedures tested

## 🎉 Next Steps After Testing

1. **Fix any identified issues**
2. **Optimize performance bottlenecks**
3. **Enhance error handling**
4. **Add additional test cases**
5. **Document lessons learned**
6. **Prepare for production deployment**

---

**🚀 Bắt đầu testing từ Phase 1 và làm theo thứ tự!**