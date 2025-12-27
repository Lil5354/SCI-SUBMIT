# Script để clear cache và rebuild project
Write-Host "=== CLEAR CACHE VÀ REBUILD PROJECT ===" -ForegroundColor Cyan

# 1. Stop application nếu đang chạy
Write-Host "`n1. Đang dừng ứng dụng..." -ForegroundColor Yellow
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*SciSubmit*"} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# 2. Xóa bin và obj folders
Write-Host "2. Đang xóa bin và obj folders..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Recurse -Force "bin"
    Write-Host "   ✓ Đã xóa bin folder" -ForegroundColor Green
}
if (Test-Path "obj") {
    Remove-Item -Recurse -Force "obj"
    Write-Host "   ✓ Đã xóa obj folder" -ForegroundColor Green
}

# 3. Clean project
Write-Host "3. Đang clean project..." -ForegroundColor Yellow
dotnet clean
Write-Host "   ✓ Đã clean project" -ForegroundColor Green

# 4. Build project
Write-Host "4. Đang build project..." -ForegroundColor Yellow
dotnet build
if ($?) {
    Write-Host "   ✓ Build thành công!" -ForegroundColor Green
} else {
    Write-Host "   ✗ Build thất bại!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== HOÀN THÀNH ===" -ForegroundColor Cyan
Write-Host "Bây giờ hãy:" -ForegroundColor Yellow
Write-Host "1. Start lại ứng dụng" -ForegroundColor White
Write-Host "2. Mở browser và nhấn Ctrl + F5 (hard refresh)" -ForegroundColor White
Write-Host "3. Hoặc mở cửa sổ ẩn danh và truy cập /Admin/Dashboard" -ForegroundColor White

















