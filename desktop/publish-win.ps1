# publish-win.ps1 — Build y empaqueta Merkatto Desktop para Windows
# Ejecutar desde la raíz del repo:  .\desktop\publish-win.ps1 -Version 1.0.0
#
# Requisitos previos (una vez):
#   dotnet tool install -g vpk
#   npm install (en frontend/)

param(
    [Parameter(Mandatory)][string]$Version,
    [string]$FeedUrl = "",         # URL del feed (vacío = sin auto-update)
    [string]$Channel = "win"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Write-Host "=== Merkatto Desktop Publish v$Version ===" -ForegroundColor Cyan

# 1. Build Angular SPA
Write-Host "`n[1/4] Building Angular..." -ForegroundColor Yellow
Push-Location "$root\frontend"
npm run build
Pop-Location

# 2. Copiar SPA al wwwroot del Desktop
$wwwroot = "$root\backend\src\Merkatto.Desktop\wwwroot"
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
Copy-Item "$root\frontend\dist\merkatto-web\browser" $wwwroot -Recurse
Write-Host "   SPA copied to wwwroot" -ForegroundColor Green

# 3. Publish .NET — self-contained, win-x64
$publishDir = "$root\desktop\publish-win"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

Write-Host "`n[2/4] Publishing .NET (win-x64, self-contained)..." -ForegroundColor Yellow
dotnet publish "$root\backend\src\Merkatto.Desktop" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PackageVersion=$Version `
    -p:AssemblyVersion=$Version `
    -p:FileVersion=$Version `
    -o $publishDir

# Inject FeedUrl into appsettings.json in the publish output if provided
if ($FeedUrl) {
    $cfg = Get-Content "$publishDir\appsettings.json" | ConvertFrom-Json
    $cfg.Updates.FeedUrl = $FeedUrl
    $cfg | ConvertTo-Json -Depth 10 | Set-Content "$publishDir\appsettings.json"
    Write-Host "   FeedUrl set to: $FeedUrl" -ForegroundColor Green
}

# 4. vpk pack
$releaseDir = "$root\desktop\releases"
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

Write-Host "`n[3/4] Packing with vpk..." -ForegroundColor Yellow
vpk pack `
    --packId     Merkatto `
    --packVersion $Version `
    --packDir    $publishDir `
    --mainExe    Merkatto.Desktop.exe `
    --packTitle  "Merkatto" `
    --packAuthors "Sebastian Levano" `
    --outputDir  $releaseDir `
    --channel    $Channel

Write-Host "`n[4/4] Done!" -ForegroundColor Green
Write-Host "Packages in: $releaseDir"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Create a GitHub Release tagged v$Version"
Write-Host "  2. Upload everything from $releaseDir to the release"
Write-Host "  3. Set FeedUrl = 'https://github.com/<owner>/<repo>/releases/' in client installs"
