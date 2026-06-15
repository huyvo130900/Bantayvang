# Testing Features - BanTayVang API

Thư mục này chứa các file và hướng dẫn để test từng tính năng của hệ thống.

## 📁 Cấu trúc thư mục

```
TestingFeatures/
├── 01-Database-Setup/          # Setup database và dữ liệu
├── 02-Question-Management/     # Test quản lý câu hỏi
├── 03-Exam-System/            # Test hệ thống thi
├── 04-Anti-Cheat/             # Test chống gian lận
├── 05-API-Testing/            # Test API endpoints
└── 06-Integration-Tests/      # Test tích hợp
```

## 🚀 Quy trình test

### Bước 1: Setup Database
1. Chạy các SQL scripts trong `01-Database-Setup/`
2. Verify dữ liệu đã được tạo

### Bước 2: Test từng tính năng
1. Question Management
2. Exam System  
3. Anti-Cheat Features
4. API Integration

### Bước 3: End-to-End Testing
1. Complete exam flow
2. Performance testing
3. Error handling

## 📋 Checklist

- [ ] Database setup hoàn tất
- [ ] API server chạy thành công
- [ ] Question CRUD works
- [ ] Exam flow works
- [ ] Anti-cheat works
- [ ] All endpoints tested

---

**Bắt đầu từ folder `01-Database-Setup/`**