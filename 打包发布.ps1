# 打包发布脚本
param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  电子元器件价格查询程序 - 打包发布" -ForegroundColor Cyan
Write-Host "  版本: $Version" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$sourceDir = "WGetCrawler-Windows"
$zipName = "WGetCrawler-v$Version-Windows-x64.zip"
$outputPath = Join-Path (Get-Location) $zipName

# 检查源目录
if (-not (Test-Path $sourceDir)) {
    Write-Host "✗ 未找到发布目录: $sourceDir" -ForegroundColor Red
    exit 1
}

# 删除旧的压缩包
if (Test-Path $outputPath) {
    Remove-Item $outputPath -Force
    Write-Host "✓ 已删除旧版本压缩包" -ForegroundColor Yellow
}

# 创建压缩包
Write-Host "正在打包..." -ForegroundColor Yellow
Compress-Archive -Path "$sourceDir\*" -DestinationPath $outputPath -CompressionLevel Optimal

# 检查结果
if (Test-Path $outputPath) {
    $size = [math]::Round((Get-Item $outputPath).Length / 1MB, 2)
    Write-Host "✓ 打包成功!" -ForegroundColor Green
    Write-Host "  文件名: $zipName" -ForegroundColor Gray
    Write-Host "  大小: $size MB" -ForegroundColor Gray
    Write-Host "  路径: $outputPath" -ForegroundColor Gray
} else {
    Write-Host "✗ 打包失败" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "发布包内容:" -ForegroundColor Yellow
Write-Host "-----------------------------------------------" -ForegroundColor Gray
Get-ChildItem $sourceDir | Select-Object Name, @{Name="Size";Expression={
    if ($_.PSIsContainer) { "[目录]" } else { 
        $size = $_.Length
        if ($size -gt 1MB) { "$([math]::Round($size/1MB,2)) MB" }
        elseif ($size -gt 1KB) { "$([math]::Round($size/1KB,2)) KB" }
        else { "$size B" }
    }
}} | Format-Table -AutoSize

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "提示: 将 $zipName 分发给用户" -ForegroundColor Green
Write-Host "用户解压后双击 '启动程序.bat' 即可使用" -ForegroundColor Green
