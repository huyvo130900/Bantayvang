# Exam System Testing

## 🎯 Mục tiêu
Test đầy đủ quy trình thi trực tuyến từ A-Z.

## 📋 Complete Exam Flow

### Phase 1: Exam Setup
- [ ] **Create Exam** - Tạo đề thi mới
- [ ] **Add Questions to Exam** - Thêm câu hỏi vào đề thi
- [ ] **Activate Exam** - Kích hoạt đề thi

### Phase 2: Student Takes Exam
- [ ] **Get Available Exams** - Lấy danh sách đề thi
- [ ] **Start Exam** - Bắt đầu làm bài
- [ ] **Get Exam Questions** - Lấy câu hỏi trong đề thi
- [ ] **Save Answers** - Lưu câu trả lời
- [ ] **Check Progress** - Kiểm tra tiến độ
- [ ] **Submit Exam** - Nộp bài

### Phase 3: Anti-Cheat Monitoring
- [ ] **Log Tab Switch** - Ghi nhận chuyển tab
- [ ] **Log Copy/Paste** - Ghi nhận copy/paste
- [ ] **Count Warnings** - Đếm số cảnh báo
- [ ] **Auto Submit on Max Warnings** - Tự động nộp khi quá nhiều cảnh báo

### Phase 4: Time Management
- [ ] **Check Remaining Time** - Kiểm tra thời gian còn lại
- [ ] **Auto Submit on Timeout** - Tự động nộp khi hết giờ
- [ ] **Handle Time Extensions** - Xử lý gia hạn thời gian

## 🧪 Test Scenarios

### Scenario 1: Happy Path
Sinh viên làm bài bình thường, không có gian lận, nộp bài đúng giờ.

### Scenario 2: Time Pressure
Sinh viên làm bài gần hết giờ, test auto-submit.

### Scenario 3: Cheating Detection
Sinh viên có hành vi gian lận, test warning system.

### Scenario 4: Technical Issues
Test xử lý lỗi mạng, mất kết nối, recovery.

## 📊 Key Metrics to Monitor

- **Response Time** - Thời gian phản hồi API
- **Data Consistency** - Tính nhất quán dữ liệu
- **Warning Accuracy** - Độ chính xác của cảnh báo
- **Auto-submit Reliability** - Độ tin cậy tự động nộp bài

## 🔧 Test Tools

- `complete-exam-flow.http` - Test quy trình hoàn chỉnh
- `time-management.http` - Test quản lý thời gian
- `anti-cheat.http` - Test chống gian lận
- `error-handling.http` - Test xử lý lỗi