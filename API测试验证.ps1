# API 接口测试验证脚本
# 用途：验证所有 API 示例的正确性
# 作者：WGetCrawler Team
# 日期：2025-11-14

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "    API 接口完整性测试验证" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:3000"
$testPassed = 0
$testFailed = 0

# 测试 1: 健康检查
Write-Host "[测试 1/5] 健康检查接口..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Method Get -Uri "$baseUrl/health" -ErrorAction Stop
    if ($health.code -eq 200) {
        Write-Host "  ✓ 健康检查通过" -ForegroundColor Green
        Write-Host "    规则数量: $($health.data.rulesCount)" -ForegroundColor Gray
        $testPassed++
    } else {
        Write-Host "  ✗ 健康检查失败: code = $($health.code)" -ForegroundColor Red
        $testFailed++
    }
} catch {
    Write-Host "  ✗ 健康检查失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "    请确保服务已启动（运行 启动程序.bat）" -ForegroundColor Yellow
    $testFailed++
    exit 1
}

Write-Host ""

# 测试 2: 提交任务接口
Write-Host "[测试 2/5] 提交查询任务接口..." -ForegroundColor Yellow
try {
    $requestBody = @{
        models = @("STM32F103C8T6")
        useCache = $true
        retryTimes = 3
    } | ConvertTo-Json

    Write-Host "  请求体: $requestBody" -ForegroundColor Gray
    
    $submitResponse = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/Crawl" `
        -ContentType "application/json" `
        -Body $requestBody -ErrorAction Stop
    
    if ($submitResponse.code -eq 200 -and $submitResponse.data.taskId) {
        Write-Host "  ✓ 任务提交成功" -ForegroundColor Green
        Write-Host "    Task ID: $($submitResponse.data.taskId)" -ForegroundColor Gray
        Write-Host "    状态: $($submitResponse.data.status)" -ForegroundColor Gray
        $global:taskId = $submitResponse.data.taskId
        $testPassed++
    } else {
        Write-Host "  ✗ 任务提交失败: $($submitResponse.message)" -ForegroundColor Red
        $testFailed++
    }
} catch {
    Write-Host "  ✗ 任务提交失败: $($_.Exception.Message)" -ForegroundColor Red
    $testFailed++
}

Write-Host ""

# 测试 3: 查询任务状态接口（立即查询）
Write-Host "[测试 3/5] 查询任务状态接口（处理中）..." -ForegroundColor Yellow
try {
    Start-Sleep -Milliseconds 500
    $statusResponse = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Tasks/$global:taskId" -ErrorAction Stop
    
    if ($statusResponse.code -eq 200) {
        Write-Host "  ✓ 状态查询成功" -ForegroundColor Green
        Write-Host "    状态: $($statusResponse.data.status)" -ForegroundColor Gray
        Write-Host "    进度: $($statusResponse.data.completedCount)/$($statusResponse.data.totalCount)" -ForegroundColor Gray
        $testPassed++
    } else {
        Write-Host "  ✗ 状态查询失败" -ForegroundColor Red
        $testFailed++
    }
} catch {
    Write-Host "  ✗ 状态查询失败: $($_.Exception.Message)" -ForegroundColor Red
    $testFailed++
}

Write-Host ""

# 测试 4: 等待任务完成
Write-Host "[测试 4/5] 等待任务完成..." -ForegroundColor Yellow
$maxWaitSeconds = 15
$waited = 0
$completed = $false

while ($waited -lt $maxWaitSeconds) {
    Start-Sleep -Seconds 1
    $waited++
    
    try {
        $result = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/Tasks/$global:taskId" -ErrorAction Stop
        
        Write-Host "  等待中... [$waited 秒] 状态: $($result.data.status)" -ForegroundColor Gray
        
        if ($result.data.status -eq "completed") {
            Write-Host "  ✓ 任务完成" -ForegroundColor Green
            $global:finalResult = $result
            $completed = $true
            $testPassed++
            break
        } elseif ($result.data.status -eq "failed") {
            Write-Host "  ✗ 任务失败" -ForegroundColor Red
            $testFailed++
            break
        }
    } catch {
        Write-Host "  查询状态时出错: $($_.Exception.Message)" -ForegroundColor Red
    }
}

if (-not $completed -and $waited -ge $maxWaitSeconds) {
    Write-Host "  ✗ 任务超时（超过 $maxWaitSeconds 秒）" -ForegroundColor Red
    $testFailed++
}

Write-Host ""

# 测试 5: 验证返回数据结构
Write-Host "[测试 5/5] 验证返回数据结构..." -ForegroundColor Yellow
if ($global:finalResult) {
    try {
        $data = $global:finalResult.data
        
        # 检查必需字段
        $requiredFields = @("taskId", "status", "totalCount", "completedCount", "results")
        $missingFields = @()
        
        foreach ($field in $requiredFields) {
            if (-not ($data.PSObject.Properties.Name -contains $field)) {
                $missingFields += $field
            }
        }
        
        if ($missingFields.Count -eq 0) {
            Write-Host "  ✓ 数据结构完整" -ForegroundColor Green
            
            # 检查结果详情
            if ($data.results.Count -gt 0) {
                $firstResult = $data.results[0]
                Write-Host "    型号: $($firstResult.modelNumber)" -ForegroundColor Gray
                Write-Host "    状态: $($firstResult.status)" -ForegroundColor Gray
                
                if ($firstResult.status -eq "success" -and $firstResult.data.price) {
                    Write-Host "    价格: ¥$($firstResult.data.price)" -ForegroundColor Cyan
                    Write-Host "    耗时: $($firstResult.duration) ms" -ForegroundColor Gray
                    $testPassed++
                } else {
                    Write-Host "  ✗ 未获取到价格数据" -ForegroundColor Red
                    if ($firstResult.errorMessage) {
                        Write-Host "    错误: $($firstResult.errorMessage)" -ForegroundColor Red
                    }
                    $testFailed++
                }
            } else {
                Write-Host "  ✗ 结果列表为空" -ForegroundColor Red
                $testFailed++
            }
        } else {
            Write-Host "  ✗ 数据结构不完整" -ForegroundColor Red
            Write-Host "    缺少字段: $($missingFields -join ', ')" -ForegroundColor Red
            $testFailed++
        }
    } catch {
        Write-Host "  ✗ 数据验证失败: $($_.Exception.Message)" -ForegroundColor Red
        $testFailed++
    }
} else {
    Write-Host "  ✗ 无结果数据" -ForegroundColor Red
    $testFailed++
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "测试完成" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "通过: $testPassed 项" -ForegroundColor Green
Write-Host "失败: $testFailed 项" -ForegroundColor Red
Write-Host ""

if ($testFailed -eq 0) {
    Write-Host "✓ 所有测试通过！API 接口工作正常。" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ 部分测试失败，请检查错误信息。" -ForegroundColor Red
    exit 1
}
