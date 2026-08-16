# ProVMS Dev Runner — kills old instance then starts fresh
Get-Process -Name "ProVMSIT15" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
dotnet run --project "$PSScriptRoot\ProVMSIT15.csproj"
