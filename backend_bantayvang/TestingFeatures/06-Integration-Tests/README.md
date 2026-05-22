# Integration Testing

## 🎯 Mục tiêu
Test tích hợp giữa các module và end-to-end workflows.

## 🔄 Integration Scenarios

### 1. Question-to-Exam Integration
- Tạo câu hỏi → Thêm vào đề thi → Sinh viên làm bài
- Verify data consistency across modules
- Test cascade operations

### 2. Exam-to-Grading Integration  
- Sinh viên nộp bài → Tự động chấm điểm → Lưu kết quả
- Test scoring algorithms
- Verify grade calculations

### 3. Anti-Cheat Integration
- Phát hiện gian lận → Ghi log → Tự động nộp bài
- Test warning thresholds
- Verify automatic actions

### 4. User-Role Integration
- Admin tạo đề thi → Giáo viên quản lý → Sinh viên làm bài
- Test permission boundaries
- Verify role-based access

## 🧪 Test Workflows

### Workflow 1: Complete Question Lifecycle
```
Create Question → Add to Exam → Student Answers → Auto Grade → View Results
```

### Workflow 2: Exam Management Lifecycle
```
Create Exam → Activate → Students Take → Monitor → Grade → Report
```

### Workflow 3: Anti-Cheat Lifecycle
```
Start Exam → Detect Cheating → Log Warnings → Auto Submit → Review
```

### Workflow 4: Multi-User Scenario
```
Multiple Students → Same Exam → Concurrent Access → Individual Results
```

## 📊 Data Flow Testing

### Database Transactions
- Test ACID properties
- Verify rollback scenarios
- Check data integrity

### Cross-Module Communication
- Service-to-Service calls
- Repository interactions
- DTO mappings

### State Management
- Exam session states
- Answer persistence
- Progress tracking

## 🔧 Test Files

- `complete-lifecycle.http` - Full question-to-result workflow
- `multi-user-scenario.http` - Concurrent user testing
- `data-consistency.http` - Data integrity testing
- `performance-integration.http` - Performance under load

## ✅ Success Criteria

- All workflows complete without errors
- Data remains consistent across operations
- Performance meets requirements
- Error handling works across modules
- User experience is seamless