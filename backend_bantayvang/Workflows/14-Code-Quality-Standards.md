# Workflow 14: Code Quality & Standards

## 📏 Tổng quan Code Quality

Workflow này thiết lập các tiêu chuẩn code quality, coding conventions, và automated quality checks.

## 🎯 Quality Goals

### Code Quality Metrics
- **Code Coverage:** ≥ 80%
- **Maintainability Index:** ≥ 70
- **Cyclomatic Complexity:** ≤ 10 per method
- **Code Duplication:** ≤ 5%
- **Technical Debt:** ≤ 30 minutes

### Performance Standards
- **Build Time:** ≤ 2 minutes
- **Test Execution:** ≤ 5 minutes
- **Static Analysis:** ≤ 1 minute

## 📋 Coding Standards

### C# Coding Conventions

```csharp
// File: CodingStandards.cs - Examples of coding standards

namespace BanTayVang.API.Standards
{
    /// <summary>
    /// Service for managing exam operations
    /// Follows single responsibility principle
    /// </summary>
    public class ExamService : IExamService
    {
        private readonly IExamRepository _examRepository;
        private readonly ILogger<ExamService> _logger;
        
        // Constructor injection - dependency inversion
        public ExamService(
            IExamRepository examRepository,
            ILogger<ExamService> logger)
        {
            _examRepository = examRepository ?? throw new ArgumentNullException(nameof(examRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Creates a new exam with validation
        /// </summary>
        /// <param name="createDto">Exam creation data</param>
        /// <returns>Created exam or error response</returns>
        public async Task<BaseResponseDto<ExamDto>> CreateExamAsync(CreateExamDto createDto)
        {
            // Input validation
            if (createDto == null)
            {
                return BaseResponseDto<ExamDto>.Failure("Create data cannot be null");
            }
            
            try
            {
                // Business logic
                var exam = await _examRepository.CreateAsync(createDto);
                
                _logger.LogInformation("Exam created successfully with ID: {ExamId}", exam.Id);
                
                return BaseResponseDto<ExamDto>.Success(exam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam");
                return BaseResponseDto<ExamDto>.Failure("Failed to create exam");
            }
        }
        
        // Private methods use camelCase
        private bool ValidateExamData(CreateExamDto createDto)
        {
            return !string.IsNullOrWhiteSpace(createDto.Title) &&
                   createDto.Duration > 0;
        }
    }
    
    // DTOs follow clear naming conventions
    public class CreateExamDto
    {
        [Required(ErrorMessage = "Exam title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;
        
        [Range(1, 300, ErrorMessage = "Duration must be between 1 and 300 minutes")]
        public int Duration { get; set; }
        
        [Required]
        public List<int> QuestionIds { get; set; } = new();
    }
    
    // Enums use PascalCase
    public enum ExamStatus
    {
        Draft,
        Active,
        Completed,
        Cancelled
    }
    
    // Constants use UPPER_CASE
    public static class ExamConstants
    {
        public const int MAX_EXAM_DURATION = 300;
        public const int MIN_QUESTIONS_PER_EXAM = 1;
        public const string DEFAULT_EXAM_STATUS = "Draft";
    }
}
```

### EditorConfig Configuration

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.{cs,csx,vb,vbx}]
indent_size = 4

[*.{json,js,ts,html,css,scss}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false

# C# specific rules
[*.cs]
# Organize usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# this. preferences
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent

# Modifier preferences
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion

# Expression-level preferences
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_coalesce_expression = true:suggestion

# C# formatting rules
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_indent_labels = flush_left

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_parentheses = false
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_binary_operators = before_and_after
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false

# Wrapping preferences
csharp_preserve_single_line_statements = true
csharp_preserve_single_line_blocks = true

# var preferences
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
```

## 🔍 Static Code Analysis

### Analyzer Configuration

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" PrivateAssets="all" />
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.12.0.78982" PrivateAssets="all" />
    <PackageReference Include="StyleCop.Analyzers" Version="1.1.118" PrivateAssets="all" />
    <PackageReference Include="Roslynator.Analyzers" Version="4.6.2" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <AdditionalFiles Include="stylecop.json" />
  </ItemGroup>
</Project>
```

### StyleCop Configuration

```json
// stylecop.json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "companyName": "BanTayVang Hospital",
      "copyrightText": "Copyright (c) {companyName}. All rights reserved.",
      "xmlHeader": true,
      "fileNamingConvention": "stylecop"
    },
    "orderingRules": {
      "usingDirectivesPlacement": "outsideNamespace",
      "elementOrder": [
        "kind",
        "accessibility",
        "constant",
        "static",
        "readonly"
      ]
    },
    "namingRules": {
      "allowCommonHungarianPrefixes": false,
      "allowedHungarianPrefixes": []
    },
    "maintainabilityRules": {
      "topLevelTypes": [
        "class",
        "interface",
        "struct",
        "delegate",
        "enum"
      ]
    },
    "layoutRules": {
      "newlineAtEndOfFile": "require",
      "allowConsecutiveUsings": true
    }
  }
}
```

### SonarQube Configuration

```xml
<!-- SonarQube.Analysis.xml -->
<?xml version="1.0" encoding="utf-8"?>
<SonarQubeAnalysisProperties xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
                             xmlns:xsd="http://www.w3.org/2001/XMLSchema" 
                             xmlns="http://www.sonarsource.com/msbuild/integration/2015/1">
  
  <Property Name="sonar.projectKey">bantayvang-api</Property>
  <Property Name="sonar.projectName">BanTayVang API</Property>
  <Property Name="sonar.projectVersion">1.0</Property>
  
  <!-- Quality Gate -->
  <Property Name="sonar.qualitygate.wait">true</Property>
  
  <!-- Coverage -->
  <Property Name="sonar.cs.opencover.reportsPaths">**/coverage.opencover.xml</Property>
  <Property Name="sonar.coverage.exclusions">
    **/*Tests.cs,
    **/*Test.cs,
    **/Program.cs,
    **/Startup.cs,
    **/Migrations/**,
    **/Models/**
  </Property>
  
  <!-- Duplications -->
  <Property Name="sonar.cpd.exclusions">
    **/Models/**,
    **/DTOs/**,
    **/Migrations/**
  </Property>
  
  <!-- Issues -->
  <Property Name="sonar.issue.ignore.multicriteria">e1,e2</Property>
  <Property Name="sonar.issue.ignore.multicriteria.e1.ruleKey">csharpsquid:S101</Property>
  <Property Name="sonar.issue.ignore.multicriteria.e1.resourceKey">**/DTOs/**</Property>
  <Property Name="sonar.issue.ignore.multicriteria.e2.ruleKey">csharpsquid:S1118</Property>
  <Property Name="sonar.issue.ignore.multicriteria.e2.resourceKey">**/Program.cs</Property>
  
</SonarQubeAnalysisProperties>
```

## 🔧 Code Quality Tools

### Pre-commit Hooks

```yaml
# .pre-commit-config.yaml
repos:
  - repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v4.4.0
    hooks:
      - id: trailing-whitespace
      - id: end-of-file-fixer
      - id: check-yaml
      - id: check-json
      - id: check-merge-conflict
      - id: check-case-conflict

  - repo: local
    hooks:
      - id: dotnet-format
        name: dotnet format
        entry: dotnet format --verify-no-changes
        language: system
        types: [c#]
        
      - id: dotnet-build
        name: dotnet build
        entry: dotnet build --configuration Release --no-restore
        language: system
        types: [c#]
        
      - id: dotnet-test
        name: dotnet test
        entry: dotnet test --configuration Release --no-build --verbosity minimal
        language: system
        types: [c#]
```

### Quality Check Script

```bash
#!/bin/bash
# quality-check.sh

set -e

echo "🔍 Running Code Quality Checks..."

# Format check
echo "📝 Checking code formatting..."
dotnet format --verify-no-changes --verbosity diagnostic

# Build check
echo "🔨 Building solution..."
dotnet build --configuration Release --no-restore --verbosity minimal

# Static analysis
echo "🔍 Running static analysis..."
dotnet build --configuration Release --verbosity minimal /p:RunAnalyzersDuringBuild=true

# Security scan
echo "🛡️ Running security scan..."
dotnet list package --vulnerable --include-transitive

# Test execution
echo "🧪 Running tests..."
dotnet test --configuration Release --no-build --verbosity minimal \
    --collect:"XPlat Code Coverage" \
    --logger "trx;LogFileName=test-results.trx"

# Coverage check
echo "📊 Checking code coverage..."
COVERAGE=$(grep -oP 'line-rate="\K[^"]*' ./TestResults/*/coverage.cobertura.xml | head -1)
THRESHOLD=0.80

if (( $(echo "$COVERAGE >= $THRESHOLD" | bc -l) )); then
    echo "✅ Coverage $COVERAGE meets threshold $THRESHOLD"
else
    echo "❌ Coverage $COVERAGE below threshold $THRESHOLD"
    exit 1
fi

# SonarQube analysis (if available)
if command -v sonar-scanner &> /dev/null; then
    echo "📈 Running SonarQube analysis..."
    sonar-scanner \
        -Dsonar.projectKey=bantayvang-api \
        -Dsonar.sources=. \
        -Dsonar.host.url=$SONAR_HOST_URL \
        -Dsonar.login=$SONAR_TOKEN
fi

echo "✅ All quality checks passed!"
```

## 📊 Code Metrics & Reporting

### Custom Code Metrics

```csharp
// Tools/CodeMetrics/MetricsCollector.cs
public class CodeMetricsCollector
{
    public class CodeMetrics
    {
        public int LinesOfCode { get; set; }
        public int CyclomaticComplexity { get; set; }
        public double MaintainabilityIndex { get; set; }
        public int ClassCoupling { get; set; }
        public int DepthOfInheritance { get; set; }
    }
    
    public static CodeMetrics AnalyzeAssembly(Assembly assembly)
    {
        var metrics = new CodeMetrics();
        
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract)
            {
                metrics.LinesOfCode += CountLinesOfCode(type);
                metrics.CyclomaticComplexity += CalculateCyclomaticComplexity(type);
                metrics.ClassCoupling += CalculateClassCoupling(type);
                metrics.DepthOfInheritance = Math.Max(metrics.DepthOfInheritance, 
                    CalculateDepthOfInheritance(type));
            }
        }
        
        metrics.MaintainabilityIndex = CalculateMaintainabilityIndex(metrics);
        
        return metrics;
    }
    
    private static int CountLinesOfCode(Type type)
    {
        // Implementation to count lines of code
        // This would typically use Roslyn analyzers
        return 0;
    }
    
    private static int CalculateCyclomaticComplexity(Type type)
    {
        // Implementation to calculate cyclomatic complexity
        return 0;
    }
    
    private static double CalculateMaintainabilityIndex(CodeMetrics metrics)
    {
        // Maintainability Index = 171 - 5.2 * ln(Halstead Volume) 
        //                        - 0.23 * (Cyclomatic Complexity) 
        //                        - 16.2 * ln(Lines of Code)
        return 100.0; // Simplified calculation
    }
}
```

### Quality Report Generator

```csharp
// Tools/Reports/QualityReportGenerator.cs
public class QualityReportGenerator
{
    public class QualityReport
    {
        public DateTime GeneratedAt { get; set; }
        public CodeMetrics Metrics { get; set; }
        public TestResults TestResults { get; set; }
        public SecurityScanResults SecurityResults { get; set; }
        public List<QualityIssue> Issues { get; set; } = new();
    }
    
    public class QualityIssue
    {
        public string Severity { get; set; }
        public string Rule { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
    }
    
    public async Task<QualityReport> GenerateReportAsync()
    {
        var report = new QualityReport
        {
            GeneratedAt = DateTime.UtcNow,
            Metrics = await CollectCodeMetricsAsync(),
            TestResults = await CollectTestResultsAsync(),
            SecurityResults = await CollectSecurityResultsAsync(),
            Issues = await CollectQualityIssuesAsync()
        };
        
        return report;
    }
    
    public async Task GenerateHtmlReportAsync(QualityReport report, string outputPath)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>BanTayVang API - Quality Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .metric {{ background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }}
        .success {{ color: green; }}
        .warning {{ color: orange; }}
        .error {{ color: red; }}
    </style>
</head>
<body>
    <h1>Code Quality Report</h1>
    <p>Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
    
    <h2>Code Metrics</h2>
    <div class='metric'>
        <strong>Lines of Code:</strong> {report.Metrics.LinesOfCode}
    </div>
    <div class='metric'>
        <strong>Cyclomatic Complexity:</strong> {report.Metrics.CyclomaticComplexity}
    </div>
    <div class='metric'>
        <strong>Maintainability Index:</strong> {report.Metrics.MaintainabilityIndex:F2}
    </div>
    
    <h2>Test Results</h2>
    <div class='metric'>
        <strong>Total Tests:</strong> {report.TestResults.Total}
    </div>
    <div class='metric'>
        <strong>Passed:</strong> <span class='success'>{report.TestResults.Passed}</span>
    </div>
    <div class='metric'>
        <strong>Failed:</strong> <span class='error'>{report.TestResults.Failed}</span>
    </div>
    <div class='metric'>
        <strong>Coverage:</strong> {report.TestResults.Coverage:P2}
    </div>
    
    <h2>Quality Issues</h2>
    <table border='1' style='width: 100%; border-collapse: collapse;'>
        <tr>
            <th>Severity</th>
            <th>Rule</th>
            <th>File</th>
            <th>Line</th>
            <th>Message</th>
        </tr>";
        
        foreach (var issue in report.Issues)
        {
            var severityClass = issue.Severity.ToLower();
            html += $@"
        <tr>
            <td class='{severityClass}'>{issue.Severity}</td>
            <td>{issue.Rule}</td>
            <td>{issue.File}</td>
            <td>{issue.Line}</td>
            <td>{issue.Message}</td>
        </tr>";
        }
        
        html += @"
    </table>
</body>
</html>";
        
        await File.WriteAllTextAsync(outputPath, html);
    }
}
```

## 🎯 Quality Gates

### GitHub Actions Quality Gate

```yaml
# .github/workflows/quality-gate.yml
name: Quality Gate

on:
  pull_request:
    branches: [ main, develop ]

jobs:
  quality-gate:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
      with:
        fetch-depth: 0
        
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Check formatting
      run: dotnet format --verify-no-changes --verbosity diagnostic
      
    - name: Build
      run: dotnet build --configuration Release --no-restore
      
    - name: Run tests with coverage
      run: |
        dotnet test --configuration Release --no-build \
          --collect:"XPlat Code Coverage" \
          --logger "trx;LogFileName=test-results.trx"
          
    - name: Check coverage threshold
      run: |
        COVERAGE=$(grep -oP 'line-rate="\K[^"]*' ./TestResults/*/coverage.cobertura.xml | head -1)
        if (( $(echo "$COVERAGE < 0.80" | bc -l) )); then
          echo "❌ Coverage $COVERAGE below 80% threshold"
          exit 1
        fi
        echo "✅ Coverage $COVERAGE meets threshold"
        
    - name: Run security scan
      run: dotnet list package --vulnerable --include-transitive
      
    - name: SonarCloud Scan
      uses: SonarSource/sonarcloud-github-action@master
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        
    - name: Quality Gate Status
      run: |
        # Wait for SonarCloud analysis
        sleep 30
        
        # Check quality gate status
        STATUS=$(curl -s -u ${{ secrets.SONAR_TOKEN }}: \
          "https://sonarcloud.io/api/qualitygates/project_status?projectKey=bantayvang-api" \
          | jq -r '.projectStatus.status')
          
        if [ "$STATUS" != "OK" ]; then
          echo "❌ Quality gate failed: $STATUS"
          exit 1
        fi
        
        echo "✅ Quality gate passed"
```

## 📋 Code Quality Checklist

### Code Standards
- [ ] EditorConfig configured and enforced
- [ ] StyleCop rules configured
- [ ] Naming conventions followed
- [ ] Code formatting consistent
- [ ] XML documentation for public APIs
- [ ] No compiler warnings
- [ ] No code analysis warnings

### Static Analysis
- [ ] SonarQube/SonarCloud configured
- [ ] Security analyzers enabled
- [ ] Code complexity within limits
- [ ] No code duplication issues
- [ ] Maintainability index > 70

### Testing Quality
- [ ] Unit test coverage ≥ 80%
- [ ] Integration tests for critical paths
- [ ] Test naming conventions followed
- [ ] Tests are independent and repeatable
- [ ] Mock usage is appropriate

### Documentation
- [ ] README.md updated
- [ ] API documentation generated
- [ ] Code comments for complex logic
- [ ] Architecture decisions documented
- [ ] Deployment guide updated

### Automation
- [ ] Pre-commit hooks configured
- [ ] CI/CD quality gates implemented
- [ ] Automated code formatting
- [ ] Automated security scanning
- [ ] Quality reports generated

---
**Next:** Workflow 15 - Documentation & Knowledge Management
**Estimated Time:** 1 week
**Priority:** Medium for long-term maintenance