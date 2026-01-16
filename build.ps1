param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "Building backend ($Runtime)..." -ForegroundColor Cyan
dotnet publish .\backend\src\SlideGenerator.Presentation\SlideGenerator.Presentation.csproj `
    -c Release `
    -r $Runtime `
    -o .\frontend\backend `
    --self-contained false

Write-Host "Building frontend..." -ForegroundColor Cyan
Push-Location .\frontend
npm install
$env:ELECTRON_BUILDER_PUBLISH = "never"
npm run build
Remove-Item Env:ELECTRON_BUILDER_PUBLISH
Pop-Location

Write-Host "Done." -ForegroundColor Green
