# Question Management Testing

## 🎯 Mục tiêu
Test đầy đủ tính năng quản lý ngân hàng câu hỏi.

## 📋 Test Cases

### ✅ CRUD Operations
- [ ] **Create Question** - Tạo câu hỏi mới
- [ ] **Read Questions** - Lấy danh sách câu hỏi
- [ ] **Update Question** - Cập nhật câu hỏi
- [ ] **Delete Question** - Xóa mềm câu hỏi

### ✅ Search & Filter
- [ ] **Search by keyword** - Tìm kiếm theo từ khóa
- [ ] **Filter by category** - Lọc theo danh mục
- [ ] **Filter by difficulty** - Lọc theo độ khó
- [ ] **Filter by department** - Lọc theo khoa/phòng
- [ ] **Pagination** - Phân trang

### ✅ Validation
- [ ] **Required fields** - Kiểm tra trường bắt buộc
- [ ] **Correct answer validation** - Phải có ít nhất 1 đáp án đúng
- [ ] **Choice validation** - Kiểm tra lựa chọn hợp lệ

### ✅ Advanced Features
- [ ] **Random questions** - Lấy câu hỏi ngẫu nhiên
- [ ] **Import from Excel** - Import từ file Excel
- [ ] **Export to Excel** - Export ra file Excel

## 🧪 Test Scripts

Sử dụng các file trong thư mục này:
- `test-crud.http` - Test CRUD operations
- `test-search-filter.http` - Test tìm kiếm và lọc
- `test-validation.http` - Test validation rules
- `test-advanced.http` - Test tính năng nâng cao

## 📊 Expected Results

### Create Question Success
```json
{
  "success": true,
  "message": "Tạo câu hỏi thành công",
  "data": {
    "id": 8,
    "noiDung": "New question content",
    "diem": 1.0,
    "danhSachLuaChon": [...]
  }
}
```

### Validation Error
```json
{
  "success": false,
  "message": "Câu hỏi trắc nghiệm phải có ít nhất 1 đáp án đúng",
  "errors": []
}
```