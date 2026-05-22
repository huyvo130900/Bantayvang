# Anti-Cheat System Testing

## 🎯 Mục tiêu
Test hệ thống chống gian lận và monitoring.

## 🚨 Types of Cheating Detection

### 1. Tab Switching
- Phát hiện khi sinh viên chuyển tab
- Ghi nhận thời gian và tần suất
- Cảnh báo khi quá nhiều lần

### 2. Copy/Paste Detection
- Phát hiện hành vi copy/paste
- Ghi nhận nội dung (nếu cần)
- Cảnh báo ngay lập tức

### 3. Right Click Blocking
- Chặn chuột phải
- Ghi nhận các lần thử right-click
- Cảnh báo khi vi phạm

### 4. Fullscreen Exit
- Phát hiện thoát fullscreen
- Yêu cầu quay lại fullscreen
- Cảnh báo nếu không tuân thủ

### 5. Multiple Warnings
- Đếm tổng số cảnh báo
- Tự động nộp bài khi > 5 cảnh báo
- Thông báo cho giám thị

## 📊 Warning Levels

### Level 1: Information (1-2 warnings)
- Hiển thị thông báo nhẹ
- Ghi log để theo dõi
- Không ảnh hưởng đến bài thi

### Level 2: Caution (3-4 warnings)
- Hiển thị cảnh báo rõ ràng
- Gửi thông báo cho giám thị
- Ghi nhận vào hồ sơ

### Level 3: Critical (5+ warnings)
- Tự động nộp bài
- Khóa tài khoản tạm thời
- Báo cáo vi phạm nghiêm trọng

## 🧪 Test Scenarios

### Scenario 1: Normal Behavior
- Không có hành vi gian lận
- Kiểm tra hệ thống không báo sai

### Scenario 2: Occasional Violations
- 1-2 vi phạm nhỏ
- Kiểm tra cảnh báo phù hợp

### Scenario 3: Repeated Violations
- 3-4 vi phạm
- Kiểm tra escalation

### Scenario 4: Severe Violations
- 5+ vi phạm
- Kiểm tra auto-submit

## 📈 Metrics to Track

- **False Positive Rate** - Tỷ lệ báo sai
- **Detection Accuracy** - Độ chính xác phát hiện
- **Response Time** - Thời gian phản hồi
- **System Performance** - Hiệu suất hệ thống