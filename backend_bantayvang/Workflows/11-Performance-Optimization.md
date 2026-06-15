# Workflow 11: Performance Optimization & Scalability

## 🚀 Tổng quan Performance

Workflow này tối ưu hóa hiệu suất hệ thống để đáp ứng yêu cầu 200+ người dùng đồng thời với response time < 2 giây.

## 📊 Performance Requirements

### Target Metrics
- **Response Time:** < 2 seconds for 95% of requests
- **Concurrent Users:** 200+ simultaneous exam takers
- **Throughput:** 1000+ requests per minute
- **Availability:** 99.9% uptime
- **Database:** < 100ms query response time

### Current Performance Issues
- No caching implemented
- N+1 query problems in Entity Framework
- No database indexing strategy
- Synchronous operations blocking threads
- No connection pooling optimization

## 🔧 Database Optimization

### 1. Database Indexing Strategy

```sql
-- Performance indexes for BanTayVang database

-- CAUHOI table indexes
CREATE NONCLUSTERED INDEX IX_CAUHOI_DanhMuc_LoaiCauHoi 
ON CAUHOI (IdDanhMuc, IdLoaiCauHoi) 
INCLUDE (NoiDung, Diem, DoKho);

CREATE NONCLUSTERED INDEX IX_CAUHOI_DoKho_KhoaPhong 
ON CAUHOI (DoKho, KhoaPhong) 
WHERE DaXoa = 0;

CREATE NONCLUSTERED INDEX IX_CAUHOI_NgayTao 
ON CAUHOI (NgayTao DESC) 
WHERE DaXoa = 0;

-- DETHI table indexes
CREATE NONCLUSTERED INDEX IX_DETHI_MaDeThi 
ON DETHI (MaDeThi) 
INCLUDE (TenDeThi, TrangThai);

CREATE NONCLUSTERED INDEX IX_DETHI_TrangThai_ThoiGian 
ON DETHI (TrangThai, ThoiGianBatDau);

-- BAITHI table indexes
CREATE NONCLUSTERED INDEX IX_BAITHI_TaiKhoan_DeThi 
ON BAITHI (IdTaiKhoan, IdDeThi) 
INCLUDE (TrangThai, ThoiGianNop);

CREATE NONCLUSTERED INDEX IX_BAITHI_TrangThai 
ON BAITHI (TrangThai) 
INCLUDE (ThoiGianNop, TongDiem);

-- CHITIETLAMBAI table indexes
CREATE NONCLUSTERED INDEX IX_CHITIETLAMBAI_BaiThi 
ON CHITIETLAMBAI (IdBaiThi) 
INCLUDE (IdCauHoi, IdLuaChonDaChon, DiemDatDuoc);

-- LUACHON table indexes
CREATE NONCLUSTERED INDEX IX_LUACHON_CauHoi 
ON LUACHON (IdCauHoi) 
INCLUDE (NoiDung, LaDapAnDung, ThuTu);

-- CANHBAOGIANLAN table indexes
CREATE NONCLUSTERED INDEX IX_CANHBAOGIANLAN_BaiThi_ThoiGian 
ON CANHBAOGIANLAN (IdBaiThi, ThoiGian DESC);

-- LOGTHAOTAC table indexes
CREATE NONCLUSTERED INDEX IX_LOGTHAOTAC_BaiThi_ThoiGian 
ON LOGTHAOTAC (IdBaiThi, ThoiGian DESC);
```

### 2. Query Optimization Service

```csharp
// Services/Interfaces/IQueryOptimizationService.cs
public interface IQueryOptimizationService
{
    IQueryable<T> OptimizeQuery<T>(IQueryable<T> query) where T : class;
    Task<PagedResultDto<T>> GetPagedOptimizedAsync<T>(
        IQueryable<T> query, 
        int page, 
        int pageSize) where T : class;
}

// Services/Impl/QueryOptimizationService.cs
public class QueryOptimizationService : IQueryOptimizationService
{
    public IQueryable<T> OptimizeQuery<T>(IQueryable<T> query) where T : class
    {
        // Add query hints and optimizations
        return query.AsNoTracking(); // For read-only queries
    }
    
    public async Task<PagedResultDto<T>> GetPagedOptimizedAsync<T>(
        IQueryable<T> query, 
        int page, 
        int pageSize) where T : class
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
            
        return new PagedResultDto<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
```

### 3. Optimized Repository Implementation

```csharp
// Repositories/Impl/OptimizedCauhoiRepository.cs
public class OptimizedCauhoiRepository : BaseRepository<Cauhoi>, ICauhoiRepository
{
    private readonly IQueryOptimizationService _queryOptimization;
    
    public OptimizedCauhoiRepository(
        BanTayVangDbContext context,
        IQueryOptimizationService queryOptimization) : base(context)
    {
        _queryOptimization = queryOptimization;
    }
    
    public async Task<PagedResultDto<Cauhoi>> GetPagedWithChoicesAsync(
        int page, 
        int pageSize, 
        string? keyword = null)
    {
        var query = _context.Cauhois
            .Include(c => c.Luachons.OrderBy(l => l.ThuTu))
            .Include(c => c.IdDanhMucNavigation)
            .Include(c => c.IdLoaiCauHoiNavigation)
            .Where(c => c.DaXoa == false);
            
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(c => c.NoiDung.Contains(keyword));
        }
        
        query = _queryOptimization.OptimizeQuery(query);
        
        return await _queryOptimization.GetPagedOptimizedAsync(query, page, pageSize);
    }
    
    public async Task<IEnumerable<Cauhoi>> GetRandomQuestionsAsync(
        int count, 
        int? danhMucId = null, 
        string? doKho = null)
    {
        var query = _context.Cauhois
            .Where(c => c.DaXoa == false);
            
        if (danhMucId.HasValue)
            query = query.Where(c => c.IdDanhMuc == danhMucId);
            
        if (!string.IsNullOrEmpty(doKho))
            query = query.Where(c => c.DoKho == doKho);
            
        // Use SQL Server TABLESAMPLE for better performance on large datasets
        return await query
            .OrderBy(c => Guid.NewGuid()) // For small datasets
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }
}
```

## 🗄️ Caching Implementation

### 1. Multi-Level Caching Strategy

```csharp
// Services/Interfaces/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemovePatternAsync(string pattern);
}

// Services/Impl/DistributedCacheService.cs
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<DistributedCacheService> _logger;
    
    public DistributedCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<DistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        // L1 Cache: Memory Cache (fastest)
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            return cachedValue;
        }
        
        // L2 Cache: Distributed Cache (Redis)
        var distributedValue = await _distributedCache.GetStringAsync(key);
        if (distributedValue != null)
        {
            var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);
            
            // Store in L1 cache for next access
            _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
            
            return deserializedValue;
        }
        
        return null;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var serializedValue = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }
        else
        {
            options.SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Default 1 hour
        }
        
        // Set in both caches
        await _distributedCache.SetStringAsync(key, serializedValue, options);
        _memoryCache.Set(key, value, expiration ?? TimeSpan.FromMinutes(5));
    }
}
```

### 2. Cached Repository Decorator

```csharp
// Repositories/Decorators/CachedCauhoiRepository.cs
public class CachedCauhoiRepository : ICauhoiRepository
{
    private readonly ICauhoiRepository _repository;
    private readonly ICacheService _cache;
    private const int CACHE_EXPIRATION_MINUTES = 30;
    
    public CachedCauhoiRepository(ICauhoiRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }
    
    public async Task<Cauhoi?> GetByIdAsync(int id)
    {
        var cacheKey = $"cauhoi:{id}";
        var cached = await _cache.GetAsync<Cauhoi>(cacheKey);
        
        if (cached != null)
            return cached;
            
        var question = await _repository.GetByIdAsync(id);
        if (question != null)
        {
            await _cache.SetAsync(cacheKey, question, TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));
        }
        
        return question;
    }
    
    public async Task<PagedResultDto<Cauhoi>> GetPagedAsync(int page, int pageSize)
    {
        var cacheKey = $"cauhoi:paged:{page}:{pageSize}";
        var cached = await _cache.GetAsync<PagedResultDto<Cauhoi>>(cacheKey);
        
        if (cached != null)
            return cached;
            
        var result = await _repository.GetPagedAsync(page, pageSize);
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10)); // Shorter cache for paged results
        
        return result;
    }
    
    public async Task<Cauhoi> CreateAsync(Cauhoi entity)
    {
        var result = await _repository.CreateAsync(entity);
        
        // Invalidate related caches
        await _cache.RemovePatternAsync("cauhoi:paged:*");
        await _cache.RemovePatternAsync($"cauhoi:danhmuc:{entity.IdDanhMuc}:*");
        
        return result;
    }
}
```

### 3. Cache Configuration

```csharp
// Program.cs - Cache configuration
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Limit memory cache size
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "BanTayVang";
});

// Register cache service
builder.Services.AddScoped<ICacheService, DistributedCacheService>();

// Register cached repositories using Decorator pattern
builder.Services.AddScoped<ICauhoiRepository>(provider =>
{
    var baseRepository = new CauhoiRepository(provider.GetService<BanTayVangDbContext>());
    var cache = provider.GetService<ICacheService>();
    return new CachedCauhoiRepository(baseRepository, cache);
});
```

## ⚡ Async/Await Optimization

### 1. Async Repository Pattern

```csharp
// Repositories/Impl/AsyncOptimizedRepository.cs
public class AsyncOptimizedRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly BanTayVangDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public AsyncOptimizedRepository(BanTayVangDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }
    
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }
    
    public async Task<T> CreateAsync(T entity)
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    public async Task<IEnumerable<T>> CreateBulkAsync(IEnumerable<T> entities)
    {
        _dbSet.AddRange(entities);
        await _context.SaveChangesAsync();
        return entities;
    }
    
    public async Task<bool> UpdateBulkAsync(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}
```

### 2. Parallel Processing Service

```csharp
// Services/Interfaces/IParallelProcessingService.cs
public interface IParallelProcessingService
{
    Task<IEnumerable<TResult>> ProcessInParallelAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<TInput, Task<TResult>> processor,
        int maxDegreeOfParallelism = 4);
}

// Services/Impl/ParallelProcessingService.cs
public class ParallelProcessingService : IParallelProcessingService
{
    public async Task<IEnumerable<TResult>> ProcessInParallelAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<TInput, Task<TResult>> processor,
        int maxDegreeOfParallelism = 4)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = inputs.Select(async input =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await processor(input);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        return await Task.WhenAll(tasks);
    }
}
```

### 3. Optimized Exam Service

```csharp
// Services/Impl/OptimizedExamService.cs
public class OptimizedExamService : IExamService
{
    private readonly IParallelProcessingService _parallelProcessing;
    private readonly ICacheService _cache;
    
    public async Task<BaseResponseDto<ExamResultDto>> SubmitExamAsync(SubmitExamDto submitDto)
    {
        // Get exam and answers in parallel
        var examTask = GetExamByIdAsync(submitDto.ExamId);
        var answersTask = GetExamAnswersAsync(submitDto.ExamId);
        
        await Task.WhenAll(examTask, answersTask);
        
        var exam = examTask.Result;
        var answers = answersTask.Result;
        
        // Grade answers in parallel
        var gradingTasks = answers.Select(async answer =>
        {
            var question = await GetQuestionAsync(answer.IdCauHoi);
            return CalculateScore(answer, question);
        });
        
        var scores = await Task.WhenAll(gradingTasks);
        
        // Calculate final result
        var result = new ExamResultDto
        {
            TotalScore = scores.Sum(),
            CorrectAnswers = scores.Count(s => s > 0),
            TotalQuestions = scores.Length
        };
        
        return new BaseResponseDto<ExamResultDto>
        {
            Success = true,
            Data = result
        };
    }
}
```

## 🔄 Connection Pooling & Database Optimization

### 1. Database Context Configuration

```csharp
// Program.cs - Optimized DbContext
builder.Services.AddDbContext<BanTayVangDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(30); // 30 seconds timeout
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        })
        .EnableSensitiveDataLogging(false) // Disable in production
        .EnableServiceProviderCaching()
        .EnableDetailedErrors(false); // Disable in production
});

// Connection pooling configuration
builder.Services.AddDbContextPool<BanTayVangDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
}, poolSize: 128); // Adjust based on expected concurrent users
```

### 2. Database Health Check

```csharp
// HealthChecks/DatabaseHealthCheck.cs
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly BanTayVangDbContext _context;
    
    public DatabaseHealthCheck(BanTayVangDbContext context)
    {
        _context = context;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("Database is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
        }
    }
}

// Program.cs - Health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddDbContextCheck<BanTayVangDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));
```

## 📊 Performance Monitoring

### 1. Performance Metrics Service

```csharp
// Services/Interfaces/IPerformanceMetricsService.cs
public interface IPerformanceMetricsService
{
    void RecordRequestDuration(string endpoint, double durationMs);
    void RecordDatabaseQuery(string query, double durationMs);
    void RecordCacheHit(string key);
    void RecordCacheMiss(string key);
    Task<PerformanceReportDto> GetPerformanceReportAsync();
}

// Services/Impl/PerformanceMetricsService.cs
public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly ConcurrentDictionary<string, List<double>> _requestMetrics = new();
    private readonly ConcurrentDictionary<string, int> _cacheHits = new();
    private readonly ConcurrentDictionary<string, int> _cacheMisses = new();
    
    public void RecordRequestDuration(string endpoint, double durationMs)
    {
        _requestMetrics.AddOrUpdate(endpoint, 
            new List<double> { durationMs },
            (key, list) => { list.Add(durationMs); return list; });
            
        if (durationMs > 2000) // Log slow requests
        {
            _logger.LogWarning($"Slow request detected: {endpoint} took {durationMs}ms");
        }
    }
    
    public async Task<PerformanceReportDto> GetPerformanceReportAsync()
    {
        return new PerformanceReportDto
        {
            AverageResponseTime = _requestMetrics.Values
                .SelectMany(v => v)
                .DefaultIfEmpty()
                .Average(),
            CacheHitRate = CalculateCacheHitRate(),
            SlowRequestCount = _requestMetrics.Values
                .SelectMany(v => v)
                .Count(d => d > 2000)
        };
    }
}
```

### 2. Performance Middleware

```csharp
// Middleware/PerformanceMiddleware.cs
public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPerformanceMetricsService _metrics;
    private readonly ILogger<PerformanceMiddleware> _logger;
    
    public PerformanceMiddleware(
        RequestDelegate next,
        IPerformanceMetricsService metrics,
        ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _metrics = metrics;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var endpoint = $"{context.Request.Method} {context.Request.Path}";
            _metrics.RecordRequestDuration(endpoint, stopwatch.ElapsedMilliseconds);
            
            // Add performance headers
            context.Response.Headers.Add("X-Response-Time", $"{stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
```

## 🧪 Performance Testing

### 1. Load Testing Configuration

```csharp
// Tests/Performance/LoadTestConfiguration.cs
public class LoadTestConfiguration
{
    public int ConcurrentUsers { get; set; } = 200;
    public TimeSpan TestDuration { get; set; } = TimeSpan.FromMinutes(10);
    public int RequestsPerSecond { get; set; } = 100;
    public string BaseUrl { get; set; } = "https://localhost:7001";
}

// Tests/Performance/ExamLoadTest.cs
[TestFixture]
public class ExamLoadTest
{
    private HttpClient _httpClient;
    private LoadTestConfiguration _config;
    
    [SetUp]
    public void Setup()
    {
        _httpClient = new HttpClient();
        _config = new LoadTestConfiguration();
    }
    
    [Test]
    public async Task LoadTest_ConcurrentExamTaking_ShouldMeetPerformanceRequirements()
    {
        var tasks = new List<Task>();
        var results = new ConcurrentBag<TimeSpan>();
        
        for (int i = 0; i < _config.ConcurrentUsers; i++)
        {
            tasks.Add(SimulateExamTaking(results));
        }
        
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assertions
        var averageResponseTime = results.Average(r => r.TotalMilliseconds);
        var p95ResponseTime = results.OrderBy(r => r.TotalMilliseconds)
            .Skip((int)(results.Count * 0.95))
            .First()
            .TotalMilliseconds;
            
        Assert.That(averageResponseTime, Is.LessThan(1000), "Average response time should be < 1 second");
        Assert.That(p95ResponseTime, Is.LessThan(2000), "95th percentile should be < 2 seconds");
    }
    
    private async Task SimulateExamTaking(ConcurrentBag<TimeSpan> results)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate exam flow
        await StartExam();
        await GetExamQuestions();
        await SubmitAnswers();
        await SubmitExam();
        
        stopwatch.Stop();
        results.Add(stopwatch.Elapsed);
    }
}
```

## 📋 Performance Optimization Checklist

### Database Optimization
- [ ] Database indexes created for all frequently queried columns
- [ ] Query optimization service implemented
- [ ] N+1 query problems resolved
- [ ] Connection pooling configured
- [ ] Database health checks implemented

### Caching Strategy
- [ ] Multi-level caching (Memory + Redis) implemented
- [ ] Cache invalidation strategy defined
- [ ] Cached repository decorators created
- [ ] Cache hit/miss metrics tracked

### Async/Await Optimization
- [ ] All I/O operations are async
- [ ] Parallel processing for independent operations
- [ ] Proper async/await usage (no .Result or .Wait())
- [ ] Bulk operations for database writes

### Monitoring & Metrics
- [ ] Performance middleware implemented
- [ ] Response time tracking
- [ ] Slow query detection
- [ ] Cache performance metrics
- [ ] Health checks configured

### Load Testing
- [ ] Load test scenarios created
- [ ] Performance benchmarks established
- [ ] Stress testing completed
- [ ] Performance regression tests

## 📊 Expected Performance Improvements

### Before Optimization
- Response Time: 3-5 seconds
- Concurrent Users: 50-100
- Database Queries: 500ms average
- Cache Hit Rate: 0%

### After Optimization
- Response Time: < 2 seconds (95th percentile)
- Concurrent Users: 200+
- Database Queries: < 100ms average
- Cache Hit Rate: 80%+

---
**Next:** Workflow 12 - Production Deployment
**Estimated Time:** 2-3 weeks
**Priority:** High for scalability