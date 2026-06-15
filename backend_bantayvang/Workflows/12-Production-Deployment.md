# Workflow 12: Production Deployment & DevOps

## 🚀 Tổng quan Production Deployment

Workflow này hướng dẫn triển khai hệ thống lên production với đầy đủ CI/CD, monitoring, và security hardening.

## 🏗️ Infrastructure Architecture

### Production Environment Setup

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  # Application
  bantayvang-api:
    image: bantayvang/api:latest
    container_name: bantayvang-api
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - ConnectionStrings__Redis=${REDIS_CONNECTION_STRING}
      - JWT__SecretKey=${JWT_SECRET_KEY}
      - JWT__Issuer=${JWT_ISSUER}
      - JWT__Audience=${JWT_AUDIENCE}
    ports:
      - "8080:80"
    depends_on:
      - sqlserver
      - redis
    networks:
      - bantayvang-network
    volumes:
      - ./logs:/app/logs
      - ./uploads:/app/uploads

  # Database
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: bantayvang-db
    restart: unless-stopped
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${DB_SA_PASSWORD}
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
      - ./database/backup:/backup
    networks:
      - bantayvang-network

  # Redis Cache
  redis:
    image: redis:7-alpine
    container_name: bantayvang-redis
    restart: unless-stopped
    command: redis-server --requirepass ${REDIS_PASSWORD}
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - bantayvang-network

  # Nginx Reverse Proxy
  nginx:
    image: nginx:alpine
    container_name: bantayvang-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./nginx/ssl:/etc/nginx/ssl
      - ./nginx/logs:/var/log/nginx
    depends_on:
      - bantayvang-api
    networks:
      - bantayvang-network

  # Monitoring
  prometheus:
    image: prom/prometheus:latest
    container_name: bantayvang-prometheus
    restart: unless-stopped
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    networks:
      - bantayvang-network

  grafana:
    image: grafana/grafana:latest
    container_name: bantayvang-grafana
    restart: unless-stopped
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
    volumes:
      - grafana-data:/var/lib/grafana
      - ./monitoring/grafana:/etc/grafana/provisioning
    networks:
      - bantayvang-network

volumes:
  sqlserver-data:
  redis-data:
  prometheus-data:
  grafana-data:

networks:
  bantayvang-network:
    driver: bridge
```

### Nginx Configuration

```nginx
# nginx/nginx.conf
events {
    worker_connections 1024;
}

http {
    upstream bantayvang_api {
        server bantayvang-api:80;
    }

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=login:10m rate=1r/s;

    # SSL Configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    server {
        listen 80;
        server_name bantayvang.hospital.vn;
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name bantayvang.hospital.vn;

        ssl_certificate /etc/nginx/ssl/bantayvang.crt;
        ssl_certificate_key /etc/nginx/ssl/bantayvang.key;

        # Security Headers
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header Referrer-Policy "no-referrer-when-downgrade" always;
        add_header Content-Security-Policy "default-src 'self' http: https: data: blob: 'unsafe-inline'" always;
        add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

        # API Routes
        location /api/ {
            limit_req zone=api burst=20 nodelay;
            
            proxy_pass http://bantayvang_api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection keep-alive;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
            
            # Timeouts
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }

        # Login endpoint with stricter rate limiting
        location /api/auth/login {
            limit_req zone=login burst=5 nodelay;
            
            proxy_pass http://bantayvang_api;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Health check
        location /health {
            proxy_pass http://bantayvang_api/health;
            access_log off;
        }

        # Static files
        location /uploads/ {
            alias /app/uploads/;
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }
}
```

## 🔄 CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy to Production

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: bantayvang/api

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: TestPassword123!
        ports:
          - 1433:1433
        options: >-
          --health-cmd="/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestPassword123! -Q 'SELECT 1'"
          --health-interval=10s
          --health-timeout=5s
          --health-retries=3

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=BanTayVangTest;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;"
        
    - name: Security Scan
      uses: securecodewarrior/github-action-add-sarif@v1
      with:
        sarif-file: 'security-scan-results.sarif'

  build-and-push:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Log in to Container Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
        
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v4
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=sha,prefix={{branch}}-
          type=raw,value=latest,enable={{is_default_branch}}
          
    - name: Build and push Docker image
      uses: docker/build-push-action@v4
      with:
        context: .
        file: ./Dockerfile.prod
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Deploy to Production
      uses: appleboy/ssh-action@v0.1.5
      with:
        host: ${{ secrets.PROD_HOST }}
        username: ${{ secrets.PROD_USER }}
        key: ${{ secrets.PROD_SSH_KEY }}
        script: |
          cd /opt/bantayvang
          docker-compose pull
          docker-compose up -d --remove-orphans
          docker system prune -f
          
    - name: Health Check
      run: |
        sleep 30
        curl -f https://bantayvang.hospital.vn/health || exit 1
        
    - name: Notify Deployment
      uses: 8398a7/action-slack@v3
      with:
        status: ${{ job.status }}
        channel: '#deployments'
        webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

### Production Dockerfile

```dockerfile
# Dockerfile.prod
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN mkdir -p /app/logs /app/uploads && chown -R appuser:appuser /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BanTayVang.API/BanTayVang.API.csproj", "BanTayVang.API/"]
RUN dotnet restore "BanTayVang.API/BanTayVang.API.csproj"

COPY . .
WORKDIR "/src/BanTayVang.API"
RUN dotnet build "BanTayVang.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BanTayVang.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "BanTayVang.API.dll"]
```

## 📊 Monitoring & Logging

### Application Logging Configuration

```csharp
// Program.cs - Production logging
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentUserName()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "/app/logs/bantayvang-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.Seq(context.Configuration.GetConnectionString("Seq"))
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(context.Configuration.GetConnectionString("Elasticsearch")))
        {
            IndexFormat = "bantayvang-logs-{0:yyyy.MM.dd}",
            AutoRegisterTemplate = true,
            NumberOfShards = 2,
            NumberOfReplicas = 1
        });
});
```

### Prometheus Metrics

```csharp
// Services/Interfaces/IMetricsService.cs
public interface IMetricsService
{
    void IncrementRequestCount(string endpoint, string method, int statusCode);
    void RecordRequestDuration(string endpoint, double durationSeconds);
    void IncrementExamStarted();
    void IncrementExamCompleted();
    void RecordActiveUsers(int count);
}

// Services/Impl/PrometheusMetricsService.cs
public class PrometheusMetricsService : IMetricsService
{
    private readonly Counter _requestCount = Metrics
        .CreateCounter("bantayvang_http_requests_total", "Total HTTP requests", 
            new[] { "endpoint", "method", "status_code" });
            
    private readonly Histogram _requestDuration = Metrics
        .CreateHistogram("bantayvang_http_request_duration_seconds", "HTTP request duration",
            new[] { "endpoint" });
            
    private readonly Counter _examStarted = Metrics
        .CreateCounter("bantayvang_exams_started_total", "Total exams started");
        
    private readonly Counter _examCompleted = Metrics
        .CreateCounter("bantayvang_exams_completed_total", "Total exams completed");
        
    private readonly Gauge _activeUsers = Metrics
        .CreateGauge("bantayvang_active_users", "Current active users");

    public void IncrementRequestCount(string endpoint, string method, int statusCode)
    {
        _requestCount.WithLabels(endpoint, method, statusCode.ToString()).Inc();
    }

    public void RecordRequestDuration(string endpoint, double durationSeconds)
    {
        _requestDuration.WithLabels(endpoint).Observe(durationSeconds);
    }

    public void IncrementExamStarted()
    {
        _examStarted.Inc();
    }

    public void IncrementExamCompleted()
    {
        _examCompleted.Inc();
    }

    public void RecordActiveUsers(int count)
    {
        _activeUsers.Set(count);
    }
}

// Middleware/MetricsMiddleware.cs
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsService _metrics;

    public MetricsMiddleware(RequestDelegate next, IMetricsService metrics)
    {
        _next = next;
        _metrics = metrics;
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
            
            var endpoint = context.Request.Path.Value ?? "unknown";
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            
            _metrics.IncrementRequestCount(endpoint, method, statusCode);
            _metrics.RecordRequestDuration(endpoint, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
```

### Prometheus Configuration

```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "alert_rules.yml"

scrape_configs:
  - job_name: 'bantayvang-api'
    static_configs:
      - targets: ['bantayvang-api:80']
    metrics_path: '/metrics'
    scrape_interval: 10s

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093
```

### Alert Rules

```yaml
# monitoring/alert_rules.yml
groups:
  - name: bantayvang_alerts
    rules:
      - alert: HighResponseTime
        expr: histogram_quantile(0.95, bantayvang_http_request_duration_seconds_bucket) > 2
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High response time detected"
          description: "95th percentile response time is {{ $value }}s"

      - alert: HighErrorRate
        expr: rate(bantayvang_http_requests_total{status_code=~"5.."}[5m]) > 0.1
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value }} errors per second"

      - alert: DatabaseConnectionFailure
        expr: up{job="bantayvang-api"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Application is down"
          description: "BanTayVang API is not responding"

      - alert: HighMemoryUsage
        expr: (node_memory_MemTotal_bytes - node_memory_MemAvailable_bytes) / node_memory_MemTotal_bytes > 0.9
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High memory usage"
          description: "Memory usage is above 90%"
```

## 🔒 Security Hardening

### Environment Variables

```bash
# .env.prod
# Database
DB_CONNECTION_STRING="Server=sqlserver,1433;Database=HeThongBanTayVang;User Id=bantayvang_user;Password=${DB_PASSWORD};TrustServerCertificate=true;Encrypt=true;"
DB_SA_PASSWORD=${DB_SA_PASSWORD}

# Redis
REDIS_CONNECTION_STRING="redis:6379,password=${REDIS_PASSWORD}"
REDIS_PASSWORD=${REDIS_PASSWORD}

# JWT
JWT_SECRET_KEY=${JWT_SECRET_KEY}
JWT_ISSUER="BanTayVang.Hospital"
JWT_AUDIENCE="BanTayVang.Users"

# Monitoring
GRAFANA_PASSWORD=${GRAFANA_PASSWORD}

# SSL
SSL_CERT_PATH="/etc/nginx/ssl/bantayvang.crt"
SSL_KEY_PATH="/etc/nginx/ssl/bantayvang.key"
```

### Security Configuration

```csharp
// Program.cs - Security hardening
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});

if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

## 🚀 Deployment Scripts

### Deployment Script

```bash
#!/bin/bash
# deploy.sh

set -e

echo "🚀 Starting BanTayVang deployment..."

# Configuration
COMPOSE_FILE="docker-compose.prod.yml"
BACKUP_DIR="/opt/bantayvang/backups"
LOG_FILE="/opt/bantayvang/deploy.log"

# Create backup
echo "📦 Creating database backup..."
docker exec bantayvang-db /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "${DB_SA_PASSWORD}" \
    -Q "BACKUP DATABASE [HeThongBanTayVang] TO DISK = '/backup/bantayvang_$(date +%Y%m%d_%H%M%S).bak'"

# Pull latest images
echo "📥 Pulling latest images..."
docker-compose -f $COMPOSE_FILE pull

# Stop services gracefully
echo "⏹️ Stopping services..."
docker-compose -f $COMPOSE_FILE stop bantayvang-api

# Start services
echo "▶️ Starting services..."
docker-compose -f $COMPOSE_FILE up -d

# Wait for health check
echo "🏥 Waiting for health check..."
sleep 30

# Verify deployment
if curl -f https://bantayvang.hospital.vn/health; then
    echo "✅ Deployment successful!"
    
    # Clean up old images
    docker image prune -f
    
    # Log success
    echo "$(date): Deployment successful" >> $LOG_FILE
else
    echo "❌ Deployment failed! Rolling back..."
    
    # Rollback
    docker-compose -f $COMPOSE_FILE down
    docker-compose -f $COMPOSE_FILE up -d
    
    # Log failure
    echo "$(date): Deployment failed, rolled back" >> $LOG_FILE
    exit 1
fi

echo "🎉 Deployment completed successfully!"
```

### Database Migration Script

```bash
#!/bin/bash
# migrate.sh

set -e

echo "🗄️ Running database migrations..."

# Wait for database to be ready
echo "⏳ Waiting for database..."
until docker exec bantayvang-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "${DB_SA_PASSWORD}" -Q "SELECT 1" > /dev/null 2>&1; do
    sleep 5
done

# Run migrations
echo "🔄 Applying migrations..."
docker exec bantayvang-api dotnet ef database update --no-build

echo "✅ Database migrations completed!"
```

## 📋 Production Deployment Checklist

### Pre-Deployment
- [ ] All tests passing
- [ ] Security scan completed
- [ ] Performance testing completed
- [ ] Database backup created
- [ ] SSL certificates valid
- [ ] Environment variables configured
- [ ] Monitoring setup verified

### Deployment
- [ ] CI/CD pipeline executed successfully
- [ ] Docker images built and pushed
- [ ] Services deployed to production
- [ ] Database migrations applied
- [ ] Health checks passing
- [ ] SSL/HTTPS working
- [ ] Rate limiting active

### Post-Deployment
- [ ] Application responding correctly
- [ ] Database connections working
- [ ] Cache (Redis) functioning
- [ ] Monitoring dashboards showing data
- [ ] Logs being collected
- [ ] Alerts configured and working
- [ ] Performance metrics within targets
- [ ] Security headers present

### Rollback Plan
- [ ] Previous version images tagged
- [ ] Database rollback scripts ready
- [ ] Rollback procedure documented
- [ ] Rollback testing completed

## 📊 Production Monitoring Dashboard

### Key Metrics to Monitor
- **Application Health**: Response time, error rate, uptime
- **Infrastructure**: CPU, memory, disk usage
- **Database**: Connection count, query performance, deadlocks
- **Cache**: Hit rate, memory usage, evictions
- **Security**: Failed login attempts, suspicious activities
- **Business**: Active users, exams started/completed, system usage

### Alerting Thresholds
- Response time > 2 seconds (95th percentile)
- Error rate > 5%
- CPU usage > 80%
- Memory usage > 90%
- Disk usage > 85%
- Database connection failures
- Cache hit rate < 70%

---
**Status:** Production Ready
**Estimated Deployment Time:** 1-2 days
**Priority:** Critical for go-live