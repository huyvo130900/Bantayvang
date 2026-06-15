# Workflow 13: Testing Strategy & Quality Assurance

## 🧪 Tổng quan Testing Strategy

Workflow này thiết lập chiến lược testing toàn diện để đảm bảo chất lượng code và hệ thống.

## 📋 Testing Pyramid

### 1. Unit Tests (70%)
**Mục tiêu:** Test từng component độc lập
**Coverage:** 80%+ code coverage

### 2. Integration Tests (20%)
**Mục tiêu:** Test tương tác giữa các components
**Coverage:** API endpoints, database operations

### 3. End-to-End Tests (10%)
**Mục tiêu:** Test user workflows hoàn chỉnh
**Coverage:** Critical user journeys

## 🔧 Unit Testing Setup

### Test Project Structure
```
BanTayVang.Tests/
├── Unit/
│   ├── Services/
│   ├── Controllers/
│   ├── Repositories/
│   └── Validators/
├── Integration/
│   ├── API/
│   ├── Database/
│   └── Cache/
├── EndToEnd/
│   ├── ExamFlow/
│   ├── QuestionManagement/
│   └── UserManagement/
└── Helpers/
    ├── TestData/
    ├── Mocks/
    └── Fixtures/
```

### Unit Test Configuration

```csharp
// BanTayVang.Tests.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="AutoFixture" Version="4.18.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Testcontainers.MsSql" Version="3.6.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BanTayVang.API\BanTayVang.API.csproj" />
  </ItemGroup>
</Project>
```

### Base Test Classes

```csharp
// Tests/Helpers/BaseUnitTest.cs
public abstract class BaseUnitTest
{
    protected readonly IFixture _fixture;
    protected readonly Mock<ILogger> _mockLogger;
    
    protected BaseUnitTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _mockLogger = new Mock<ILogger>();
    }
    
    protected T CreateMock<T>() where T : class
    {
        return _fixture.Create<T>();
    }
}

// Tests/Helpers/DatabaseTestBase.cs
public abstract class DatabaseTestBase : IDisposable
{
    protected readonly BanTayVangDbContext _context;
    protected readonly IMapper _mapper;
    
    protected DatabaseTestBase()
    {
        var options = new DbContextOptionsBuilder<BanTayVangDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new BanTayVangDbContext(options);
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        
        SeedTestData();
    }
    
    protected virtual void SeedTestData()
    {
        // Override in derived classes to add specific test data
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}
```

## 🧪 Service Unit Tests

### ExamService Tests

```csharp
// Tests/Unit/Services/ExamServiceTests.cs
[TestFixture]
public class ExamServiceTests : BaseUnitTest
{
    private ExamService _examService;
    private Mock<IDethiRepository> _mockDethiRepository;
    private Mock<IBaithiRepository> _mockBaithiRepository;
    private Mock<IMapper> _mockMapper;
    
    [SetUp]
    public void Setup()
    {
        _mockDethiRepository = new Mock<IDethiRepository>();
        _mockBaithiRepository = new Mock<IBaithiRepository>();
        _mockMapper = new Mock<IMapper>();
        
        _examService = new ExamService(
            _mockDethiRepository.Object,
            _mockBaithiRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task CreateExamAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var createDto = _fixture.Create<CreateDethiDto>();
        var dethi = _fixture.Create<Dethi>();
        var dethiDto = _fixture.Create<DethiDto>();
        
        _mockMapper.Setup(m => m.Map<Dethi>(createDto)).Returns(dethi);
        _mockDethiRepository.Setup(r => r.CreateAsync(dethi)).ReturnsAsync(dethi);
        _mockMapper.Setup(m => m.Map<DethiDto>(dethi)).Returns(dethiDto);
        
        // Act
        var result = await _examService.CreateExamAsync(createDto);
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(dethiDto);
        
        _mockDethiRepository.Verify(r => r.CreateAsync(It.IsAny<Dethi>()), Times.Once);
    }
    
    [Test]
    public async Task StartExamAsync_WithInvalidExamCode_ShouldReturnFailure()
    {
        // Arrange
        var startDto = new StartExamDto { MaDeThi = "INVALID_CODE", IdTaiKhoan = 1 };
        
        _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync("INVALID_CODE"))
            .ReturnsAsync((Dethi?)null);
        
        // Act
        var result = await _examService.StartExamAsync(startDto);
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Không tìm thấy đề thi");
    }
    
    [Test]
    public async Task SubmitExamAsync_WithValidAnswers_ShouldCalculateCorrectScore()
    {
        // Arrange
        var submitDto = _fixture.Create<SubmitExamDto>();
        var baithi = _fixture.Create<Baithi>();
        var chitietlambais = _fixture.CreateMany<Chitietlambai>(5).ToList();
        
        // Setup correct answers
        chitietlambais[0].IdLuaChonDaChon = 1; // Correct
        chitietlambais[1].IdLuaChonDaChon = 2; // Wrong
        chitietlambais[2].IdLuaChonDaChon = 3; // Correct
        
        _mockBaithiRepository.Setup(r => r.GetByIdAsync(submitDto.IdBaiThi))
            .ReturnsAsync(baithi);
        // ... more setup
        
        // Act
        var result = await _examService.SubmitExamAsync(submitDto);
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.SoCauDung.Should().Be(2); // 2 correct answers
    }
    
    [TestCase("", "Mã đề thi không được để trống")]
    [TestCase(null, "Mã đề thi không được để trống")]
    [TestCase("TOOLONG123456789012345678901234567890", "Mã đề thi quá dài")]
    public async Task CreateExamAsync_WithInvalidMaDeThi_ShouldReturnValidationError(
        string maDeThi, string expectedError)
    {
        // Arrange
        var createDto = _fixture.Build<CreateDethiDto>()
            .With(x => x.MaDeThi, maDeThi)
            .Create();
        
        // Act
        var result = await _examService.CreateExamAsync(createDto);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(expectedError);
    }
}
```

### Repository Tests

```csharp
// Tests/Unit/Repositories/CauhoiRepositoryTests.cs
[TestFixture]
public class CauhoiRepositoryTests : DatabaseTestBase
{
    private CauhoiRepository _repository;
    
    [SetUp]
    public void Setup()
    {
        _repository = new CauhoiRepository(_context);
    }
    
    protected override void SeedTestData()
    {
        var danhMuc = new Danhmucauhoi { Id = 1, TenDanhMuc = "Test Category" };
        var loaiCauHoi = new Loaicauhoi { Id = 1, TenLoai = "Multiple Choice" };
        
        _context.Danhmucauhois.Add(danhMuc);
        _context.Loaicauhois.Add(loaiCauHoi);
        
        var questions = new List<Cauhoi>
        {
            new() { Id = 1, NoiDung = "Question 1", IdDanhMuc = 1, IdLoaiCauHoi = 1, DaXoa = false },
            new() { Id = 2, NoiDung = "Question 2", IdDanhMuc = 1, IdLoaiCauHoi = 1, DaXoa = false },
            new() { Id = 3, NoiDung = "Deleted Question", IdDanhMuc = 1, IdLoaiCauHoi = 1, DaXoa = true }
        };
        
        _context.Cauhois.AddRange(questions);
        _context.SaveChanges();
    }
    
    [Test]
    public async Task GetAllAsync_ShouldReturnOnlyActiveQuestions()
    {
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(q => q.DaXoa == false);
    }
    
    [Test]
    public async Task SearchAsync_WithKeyword_ShouldReturnMatchingQuestions()
    {
        // Act
        var result = await _repository.SearchAsync("Question 1");
        
        // Assert
        result.Should().HaveCount(1);
        result.First().NoiDung.Should().Contain("Question 1");
    }
    
    [Test]
    public async Task CreateAsync_WithValidQuestion_ShouldAddToDatabase()
    {
        // Arrange
        var newQuestion = new Cauhoi
        {
            NoiDung = "New Question",
            IdDanhMuc = 1,
            IdLoaiCauHoi = 1,
            Diem = 1.0f,
            DaXoa = false
        };
        
        // Act
        var result = await _repository.CreateAsync(newQuestion);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        
        var savedQuestion = await _context.Cauhois.FindAsync(result.Id);
        savedQuestion.Should().NotBeNull();
        savedQuestion!.NoiDung.Should().Be("New Question");
    }
}
```

## 🔗 Integration Tests

### API Integration Tests

```csharp
// Tests/Integration/API/ExamControllerIntegrationTests.cs
[TestFixture]
public class ExamControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public ExamControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Test]
    public async Task CreateExam_WithValidData_ShouldReturn201()
    {
        // Arrange
        var createDto = new CreateDethiDto
        {
            MaDeThi = "TEST001",
            TenDeThi = "Test Exam",
            ThoiGianLamBai = 60,
            NguoiTao = 1
        };
        
        var json = JsonSerializer.Serialize(createDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/exam", content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BaseResponseDto<DethiDto>>(responseContent);
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.MaDeThi.Should().Be("TEST001");
    }
    
    [Test]
    public async Task GetExam_WithInvalidId_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/exam/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Test]
    public async Task StartExam_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var startDto = new StartExamDto { MaDeThi = "TEST001", IdTaiKhoan = 1 };
        var json = JsonSerializer.Serialize(startDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/exam/start", content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Database Integration Tests

```csharp
// Tests/Integration/Database/DatabaseIntegrationTests.cs
[TestFixture]
public class DatabaseIntegrationTests
{
    private MsSqlContainer _msSqlContainer;
    private BanTayVangDbContext _context;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _msSqlContainer = new MsSqlBuilder()
            .WithPassword("TestPassword123!")
            .Build();
            
        await _msSqlContainer.StartAsync();
        
        var connectionString = _msSqlContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<BanTayVangDbContext>()
            .UseSqlServer(connectionString)
            .Options;
            
        _context = new BanTayVangDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }
    
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _context.DisposeAsync();
        await _msSqlContainer.DisposeAsync();
    }
    
    [Test]
    public async Task Database_ShouldSupportComplexQueries()
    {
        // Arrange - Create test data
        var danhMuc = new Danhmucauhoi { TenDanhMuc = "Integration Test" };
        _context.Danhmucauhois.Add(danhMuc);
        await _context.SaveChangesAsync();
        
        var questions = Enumerable.Range(1, 100)
            .Select(i => new Cauhoi
            {
                NoiDung = $"Question {i}",
                IdDanhMuc = danhMuc.Id,
                IdLoaiCauHoi = 1,
                DaXoa = false
            })
            .ToList();
            
        _context.Cauhois.AddRange(questions);
        await _context.SaveChangesAsync();
        
        // Act - Complex query with joins and pagination
        var result = await _context.Cauhois
            .Include(c => c.IdDanhMucNavigation)
            .Where(c => c.DaXoa == false)
            .OrderBy(c => c.NoiDung)
            .Skip(10)
            .Take(20)
            .ToListAsync();
        
        // Assert
        result.Should().HaveCount(20);
        result.All(c => c.IdDanhMucNavigation != null).Should().BeTrue();
    }
}
```

## 🎭 End-to-End Tests

### Complete Exam Flow Test

```csharp
// Tests/EndToEnd/ExamFlowTests.cs
[TestFixture]
public class ExamFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    [Test]
    public async Task CompleteExamFlow_ShouldWorkEndToEnd()
    {
        // Step 1: Create exam
        var createExamDto = new CreateDethiDto
        {
            MaDeThi = "E2E_TEST_001",
            TenDeThi = "End-to-End Test Exam",
            ThoiGianLamBai = 30,
            NguoiTao = 1
        };
        
        var createResponse = await PostAsync("/api/exam", createExamDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var examResult = await DeserializeAsync<BaseResponseDto<DethiDto>>(createResponse);
        var examId = examResult.Data!.Id;
        
        // Step 2: Add questions to exam
        for (int i = 1; i <= 5; i++)
        {
            var addQuestionResponse = await PostAsync($"/api/exam/{examId}/questions", 
                new { QuestionId = i, Weight = 1.0 });
            addQuestionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        
        // Step 3: Start exam
        var startExamDto = new StartExamDto
        {
            MaDeThi = "E2E_TEST_001",
            IdTaiKhoan = 1
        };
        
        var startResponse = await PostAsync("/api/exam/start", startExamDto);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var startResult = await DeserializeAsync<BaseResponseDto<BaithiDto>>(startResponse);
        var examSessionId = startResult.Data!.Id;
        
        // Step 4: Get exam questions
        var questionsResponse = await _client.GetAsync($"/api/exam/{examId}/questions");
        questionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var questionsResult = await DeserializeAsync<BaseResponseDto<IEnumerable<ExamQuestionDto>>>(questionsResponse);
        var questions = questionsResult.Data!.ToList();
        
        questions.Should().HaveCount(5);
        
        // Step 5: Submit answers
        foreach (var question in questions)
        {
            var answerDto = new SubmitAnswerDto
            {
                IdBaiThi = examSessionId,
                IdCauHoi = question.Id,
                IdLuaChonDaChon = question.Choices.First().Id // Select first choice
            };
            
            var answerResponse = await PostAsync("/api/exam/answer", answerDto);
            answerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        
        // Step 6: Submit exam
        var submitDto = new SubmitExamDto { IdBaiThi = examSessionId };
        var submitResponse = await PostAsync("/api/exam/submit", submitDto);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var submitResult = await DeserializeAsync<BaseResponseDto<ExamResultDto>>(submitResponse);
        
        // Verify final result
        submitResult.Data.Should().NotBeNull();
        submitResult.Data!.TotalQuestions.Should().Be(5);
        submitResult.Data.TotalScore.Should().BeGreaterThan(0);
    }
    
    private async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(url, content);
    }
    
    private async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
```

## 📊 Test Coverage & Quality

### Coverage Configuration

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <CoverletOutput>./coverage/</CoverletOutput>
    <Exclude>[*]*.Migrations.*,[*]*.Program,[*]*.Startup</Exclude>
    <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
  </PropertyGroup>
</Project>
```

### Test Execution Script

```bash
#!/bin/bash
# run-tests.sh

echo "🧪 Running BanTayVang Test Suite..."

# Clean previous results
rm -rf ./TestResults
rm -rf ./coverage

# Run unit tests
echo "📝 Running Unit Tests..."
dotnet test BanTayVang.Tests/BanTayVang.Tests.csproj \
    --configuration Release \
    --logger "trx;LogFileName=unit-tests.trx" \
    --collect:"XPlat Code Coverage" \
    --filter "Category=Unit"

# Run integration tests
echo "🔗 Running Integration Tests..."
dotnet test BanTayVang.Tests/BanTayVang.Tests.csproj \
    --configuration Release \
    --logger "trx;LogFileName=integration-tests.trx" \
    --collect:"XPlat Code Coverage" \
    --filter "Category=Integration"

# Run E2E tests
echo "🎭 Running End-to-End Tests..."
dotnet test BanTayVang.Tests/BanTayVang.Tests.csproj \
    --configuration Release \
    --logger "trx;LogFileName=e2e-tests.trx" \
    --filter "Category=E2E"

# Generate coverage report
echo "📊 Generating Coverage Report..."
reportgenerator \
    -reports:"./TestResults/*/coverage.cobertura.xml" \
    -targetdir:"./coverage" \
    -reporttypes:"Html;Badges"

# Check coverage threshold
echo "✅ Checking Coverage Threshold..."
COVERAGE=$(grep -oP 'line-rate="\K[^"]*' ./coverage/Cobertura.xml | head -1)
THRESHOLD=0.80

if (( $(echo "$COVERAGE >= $THRESHOLD" | bc -l) )); then
    echo "✅ Coverage $COVERAGE meets threshold $THRESHOLD"
else
    echo "❌ Coverage $COVERAGE below threshold $THRESHOLD"
    exit 1
fi

echo "🎉 All tests completed successfully!"
```

## 📋 Testing Checklist

### Unit Tests
- [ ] Service layer tests (80%+ coverage)
- [ ] Repository layer tests
- [ ] Controller tests (action methods)
- [ ] Validator tests
- [ ] Utility/Helper tests
- [ ] Exception handling tests
- [ ] Edge case tests

### Integration Tests
- [ ] API endpoint tests
- [ ] Database integration tests
- [ ] Cache integration tests
- [ ] External service integration tests
- [ ] Authentication/Authorization tests

### End-to-End Tests
- [ ] Complete exam flow
- [ ] Question management workflow
- [ ] User authentication flow
- [ ] Error handling scenarios
- [ ] Performance under load

### Test Quality
- [ ] Tests are independent
- [ ] Tests are repeatable
- [ ] Tests have clear naming
- [ ] Tests follow AAA pattern (Arrange, Act, Assert)
- [ ] Mocks are used appropriately
- [ ] Test data is isolated

---
**Next:** Workflow 14 - Code Quality & Standards
**Estimated Time:** 1-2 weeks
**Priority:** High for maintainability