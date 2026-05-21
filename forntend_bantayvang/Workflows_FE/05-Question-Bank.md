# Workflow 05: Question Bank

## Mục tiêu
Quản lý ngân hàng câu hỏi: CRUD, import từ Excel, upload hình ảnh, filter/search, random questions.

## Features
- Danh sách câu hỏi với pagination, filter (danh mục, loại, độ khó, khoa/phòng)
- Tạo câu hỏi mới (nội dung + lựa chọn + đáp án đúng)
- Sửa câu hỏi
- Xóa mềm (soft delete)
- Import câu hỏi từ file Excel
- Download template Excel
- Upload hình ảnh cho câu hỏi
- Quản lý danh mục câu hỏi (CRUD)
- Quản lý loại câu hỏi (CRUD)

## API Endpoints (từ BE)
```
# Câu hỏi
GET    /api/cauhoi?idDanhMuc=&idLoaiCauHoi=&doKho=&khoaPhong=&searchKeyword=&pageNumber=1&pageSize=10
GET    /api/cauhoi/:id
POST   /api/cauhoi
PUT    /api/cauhoi/:id
DELETE /api/cauhoi/:id
POST   /api/cauhoi/import          (multipart/form-data)
GET    /api/cauhoi/import-template  (download xlsx)
GET    /api/cauhoi/random?count=10&danhMucId=

# Danh mục
GET    /api/category/categories
POST   /api/category/categories
PUT    /api/category/categories/:id
DELETE /api/category/categories/:id

# Loại câu hỏi
GET    /api/category/types
POST   /api/category/types
PUT    /api/category/types/:id
DELETE /api/category/types/:id

# Upload
POST   /api/upload/image?folder=questions
DELETE /api/upload?fileUrl=
```

## Files cần tạo
```
src/features/questions/
├── api.ts
├── types.ts
├── schemas.ts
├── components/
│   ├── question-table.tsx        # DataTable câu hỏi
│   ├── question-form-dialog.tsx  # Dialog tạo/sửa (nội dung + lựa chọn)
│   ├── question-filter.tsx       # Filter bar
│   ├── choice-editor.tsx         # Editor cho danh sách lựa chọn
│   ├── import-excel-dialog.tsx   # Dialog import Excel
│   ├── image-upload.tsx          # Upload hình ảnh
│   ├── category-manager.tsx      # CRUD danh mục (inline)
│   └── question-type-manager.tsx # CRUD loại câu hỏi
└── pages/
    └── questions-page.tsx
```

## Question Form Structure
```
- Nội dung câu hỏi (textarea/rich text)
- Hình ảnh (upload, preview)
- Danh mục (select)
- Loại câu hỏi (select)
- Độ khó (select: Dễ, Trung bình, Khó)
- Điểm (number)
- Khoa/Phòng (input)
- Danh sách lựa chọn:
  - [x] Lựa chọn 1 (đáp án đúng)
  - [ ] Lựa chọn 2
  - [ ] Lựa chọn 3
  - [ ] Lựa chọn 4
  - [+ Thêm lựa chọn]
```

## Kết quả
- Quản lý câu hỏi đầy đủ CRUD
- Import hàng loạt từ Excel
- Upload hình ảnh
- Filter/search mạnh mẽ
- Quản lý danh mục & loại câu hỏi

## Tiếp theo → Workflow 06: Exam Management
