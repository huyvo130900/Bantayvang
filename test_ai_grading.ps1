# ============================================================
# TEST PLAN: AI Cham Tu Luan - Bu?c kiem tra tu dong
# ============================================================
# Su dung: Thay YOUR_KEY_HERE bang Gemini API Key dung
# Chay: pwsh test_ai_grading.ps1
# ============================================================

$API_BASE = "http://localhost:5062/api"
$GEMINI_KEY = "PLACEHOLDER_KEY"  # Thay bang key that

$pass = 0
$fail = 0

function Test-Step($name, $result) {
    if ($result) {
        Write-Host "[PASS] $name" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "[FAIL] $name" -ForegroundColor Red
        $script:fail++
    }
}

Write-Host "`n=== TEST 1: Gemini API Key hop le ===" -ForegroundColor Cyan
$body = @{ contents = @(@{ parts = @(@{ text = "Reply with just: OK" }) }) } | ConvertTo-Json -Depth 5
try {
    $r = Invoke-RestMethod "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=$GEMINI_KEY" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 15
    Test-Step "Gemini API key hop le" ($r.candidates[0].content.parts[0].text -match "OK")
} catch {
    Test-Step "Gemini API key hop le" $false
    Write-Host "  Chi tiet: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n=== TEST 2: Backend dang chay ===" -ForegroundColor Cyan
try {
    $h = Invoke-RestMethod "$API_BASE/../health" -TimeoutSec 5
    Test-Step "Backend health check" $true
} catch {
    Test-Step "Backend health check" $false
    Write-Host "  Backend chua chay. Chay 'dotnet run' truoc." -ForegroundColor Yellow
}

Write-Host "`n=== TEST 3: Dang nhap lay token ===" -ForegroundColor Cyan
$loginBody = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod "$API_BASE/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -TimeoutSec 10
    $token = $loginResp.data.token
    Test-Step "Login thanh cong, co token" ($token -ne $null -and $token.Length -gt 10)
    $headers = @{ Authorization = "Bearer $token" }
} catch {
    Test-Step "Login thanh cong" $false
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Yellow
    $headers = @{}
}

Write-Host "`n=== TEST 4: Lay danh sach cau tu luan chua cham ===" -ForegroundColor Cyan
try {
    $pendingResp = Invoke-RestMethod "$API_BASE/grading/pending-essay-answers?ungradedOnly=true" -Headers $headers -TimeoutSec 10
    $pendingItems = $pendingResp.data
    Test-Step "API pending-essay-answers hoat dong" ($pendingResp.success -eq $true)
    Write-Host "  Tim thay $($pendingItems.Count) cau tu luan chua cham" -ForegroundColor Gray
    
    # Kiem tra co field SuggestedAnswer moi them khong
    if ($pendingItems.Count -gt 0) {
        $firstItem = $pendingItems[0]
        Test-Step "Response co field SuggestedAnswer" ($firstItem.PSObject.Properties.Name -contains "suggestedAnswer")
        Test-Step "Response co field AiScore" ($firstItem.PSObject.Properties.Name -contains "aiScore")
        Test-Step "Response co field AiGradingStatus" ($firstItem.PSObject.Properties.Name -contains "aiGradingStatus")
        $submissionDetailId = $firstItem.submissionDetailId
        Write-Host "  Su dung SubmissionDetailId=$submissionDetailId de test" -ForegroundColor Gray
    } else {
        Write-Host "  Khong co cau tu luan chua cham, bo qua test 5-7" -ForegroundColor Yellow
        $submissionDetailId = $null
    }
} catch {
    Test-Step "API pending-essay-answers hoat dong" $false
    $submissionDetailId = $null
}

Write-Host "`n=== TEST 5: Gui yeu cau AI cham batch ===" -ForegroundColor Cyan
if ($submissionDetailId -ne $null) {
    $batchBody = @{ submissionDetailIds = @($submissionDetailId) } | ConvertTo-Json
    try {
        $batchResp = Invoke-RestMethod "$API_BASE/grading/ai-grade-batch" -Method Post -Body $batchBody -ContentType "application/json" -Headers $headers -TimeoutSec 10
        Test-Step "POST ai-grade-batch thanh cong (202/200)" ($batchResp.success -eq $true)
        Write-Host "  Message: $($batchResp.message)" -ForegroundColor Gray
    } catch {
        Test-Step "POST ai-grade-batch thanh cong" $false
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    Write-Host "`n=== TEST 6: Cho 20s roi kiem tra ket qua AI ===" -ForegroundColor Cyan
    Write-Host "  Dang cho Worker xu ly (20 giay)..." -ForegroundColor Gray
    Start-Sleep -Seconds 20
    
    try {
        $detailResp = Invoke-RestMethod "$API_BASE/grading/result/$($pendingResp.data[0].examSubmissionId)" -Headers $headers -TimeoutSec 10
        $answer = $detailResp.data.answers | Where-Object { $_.submissionDetailId -eq $submissionDetailId }
        Test-Step "SubmissionDetail co ScoreObtained sau AI cham" ($answer.scoreObtained -ne $null)
        Write-Host "  scoreObtained = $($answer.scoreObtained)" -ForegroundColor Gray
    } catch {
        Test-Step "Kiem tra ket qua sau cham" $false
    }
    
    Write-Host "`n=== TEST 7: Kiem tra DB co AiScore khong ===" -ForegroundColor Cyan
    try {
        $pendingAfter = Invoke-RestMethod "$API_BASE/grading/pending-essay-answers" -Headers $headers -TimeoutSec 10
        $item = $pendingAfter.data | Where-Object { $_.submissionDetailId -eq $submissionDetailId }
        if ($item) {
            Test-Step "AiGradingStatus = Done" ($item.aiGradingStatus -eq "Done")
            Test-Step "AiScore co gia tri (0, 0.5 hoac 1)" ($item.aiScore -ne $null -and $item.aiScore -in @(0, 0.5, 1))
            Write-Host "  aiScore=$($item.aiScore), aiGradingStatus=$($item.aiGradingStatus)" -ForegroundColor Gray
            Write-Host "  aiComment=$($item.aiComment)" -ForegroundColor Gray
        } else {
            Write-Host "  Khong tim thay item trong pending (co the da duoc cham roi)" -ForegroundColor Yellow
        }
    } catch {
        Test-Step "Kiem tra AiScore trong DB" $false
    }
} else {
    Write-Host "  Bo qua (khong co du lieu test)" -ForegroundColor Yellow
}

Write-Host "`n=== TEST 8: Edge case - empty list ===" -ForegroundColor Cyan
try {
    $emptyBody = @{ submissionDetailIds = @() } | ConvertTo-Json
    $emptyResp = Invoke-RestMethod "$API_BASE/grading/ai-grade-batch" -Method Post -Body $emptyBody -ContentType "application/json" -Headers $headers -TimeoutSec 10 -SkipHttpErrorCheck
    # Phai tra 400 hoac success=false
    Test-Step "Empty list tra ve loi hop le" (-not $emptyResp.success -or $LASTEXITCODE -eq 400)
} catch {
    Test-Step "Empty list tra ve 400 Bad Request" ($_.Exception.Response.StatusCode -eq 400)
}

Write-Host "`n=== TEST 9: Validation - qua 200 items ===" -ForegroundColor Cyan
try {
    $bigList = 1..201 | ForEach-Object { $_ }
    $bigBody = @{ submissionDetailIds = $bigList } | ConvertTo-Json
    $bigResp = Invoke-RestMethod "$API_BASE/grading/ai-grade-batch" -Method Post -Body $bigBody -ContentType "application/json" -Headers $headers -TimeoutSec 10 -SkipHttpErrorCheck
    Test-Step "Vuot 200 items tra ve loi" (-not $bigResp.success)
} catch {
    Test-Step "Vuot 200 items tra ve 400" ($_.Exception.Response.StatusCode -eq 400)
}

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "KET QUA: PASS=$pass | FAIL=$fail" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Yellow" })
Write-Host "============================================`n"
