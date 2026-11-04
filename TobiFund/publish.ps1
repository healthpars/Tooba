#requires -Version 5.1
# اسکریپت Publish خودکار — صندوق طوبی
# خروجی: ./publish/TobiFund.exe  (Self-Contained Single-File برای win-x64)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "=== Publish صندوق طوبی (win-x64, Self-Contained, Single-File) ===" -ForegroundColor Green

dotnet publish "$root/src/TobiFund.WPF/TobiFund.WPF.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$root/publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish FAILED" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Publish با موفقیت انجام شد: $root/publish/TobiFund.exe" -ForegroundColor Green
Write-Host "برای ساخت Installer، اسکریپت installer/TobiFundSetup.iss را با Inno Setup Compile کنید." -ForegroundColor Cyan
