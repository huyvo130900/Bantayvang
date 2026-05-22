# API Testing & Integration

## 🎯 Mục tiêu
Test tất cả API endpoints và tích hợp giữa các module.

## 📋 API Endpoints Coverage

### Question Management APIs
- `GET /api/cauhoi` - List questions with filters
- `GET /api/cauhoi/{id}` - Get question by ID
- `POST /api/cauhoi` - Create new question
- `PUT /api/cauhoi/{id}` - Update question
- `DELETE /api/cauhoi/{id}` - Delete question
- `GET /api/cauhoi/random` - Get random questions

### Exam Management APIs
- `GET /api/exam/active` - Get active exams
- `GET /api/exam/code/{code}` - Get exam by code
- `POST /api/exam` - Create new exam
- `POST /api/exam/start` - Start exam session
- `GET /api/exam/{id}/questions` - Get exam questions
- `GET /api/exam/{id}/progress` - Get exam progress
- `POST /api/exam/answer` - Save answer
- `POST /api/exam/submit` - Submit exam

### Anti-Cheat APIs
- `POST /api/exam/warning` - Log cheating warning
- `GET /api/exam/{id}/warnings` - Get warning count

## 🧪 Test Categories

### 1. Functional Testing
- ✅ Happy path scenarios
- ✅ Edge cases
- ✅ Error handling
- ✅ Input validation

### 2. Integration Testing
- ✅ Cross-module interactions
- ✅ Database transactions
- ✅ Data consistency

### 3. Performance Testing
- ✅ Response times
- ✅ Concurrent users
- ✅ Load testing

### 4. Security Testing
- ✅ Input sanitization
- ✅ SQL injection prevention
- ✅ XSS protection
- ✅ CORS validation

## 📊 Expected Response Formats

### Success Response
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... },
  "errors": []
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "data": null,
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

### Paginated Response
```json
{
  "success": true,
  "message": "Data retrieved successfully",
  "data": {
    "items": [...],
    "pagination": {
      "pageNumber": 1,
      "pageSize": 10,
      "totalRecords": 25,
      "totalPages": 3
    }
  }
}
```

## 🔧 Test Tools

- `api-endpoints-test.http` - Test all endpoints
- `integration-test.http` - Test cross-module integration
- `performance-test.http` - Basic performance testing
- `error-handling-test.http` - Test error scenarios