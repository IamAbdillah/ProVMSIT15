# ProVMS Full Startup Script
# Run this after every PC restart to bring everything up

Write-Host "Starting MySQL..." -ForegroundColor Cyan
Get-Process -Name "mysqld" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-ChildItem "c:\xampp\mysql\data\*.pid" -ErrorAction SilentlyContinue | Remove-Item -Force
Start-Process "c:\xampp\mysql\bin\mysqld.exe" -ArgumentList "--datadir=c:/xampp/mysql/data --port=3306 --bind-address=0.0.0.0" -WindowStyle Hidden
Start-Sleep -Seconds 6
$mysql = netstat -ano | findstr "0.0.0.0:3306"
if ($mysql) { Write-Host "MySQL is UP on port 3306" -ForegroundColor Green }
else         { Write-Host "MySQL FAILED to start" -ForegroundColor Red; exit 1 }

Write-Host "Starting Apache (phpMyAdmin)..." -ForegroundColor Cyan
Get-Process -Name "httpd" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
Start-Process "c:\xampp\apache\bin\httpd.exe" -WindowStyle Hidden
Start-Sleep -Seconds 3
Write-Host "Apache started" -ForegroundColor Green

Write-Host "Starting ProVMS app..." -ForegroundColor Cyan
Get-Process -Name "ProVMSIT15" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
Write-Host "ProVMS running at http://localhost:5239" -ForegroundColor Green
dotnet run --project "$PSScriptRoot\ProVMSIT15.csproj"
