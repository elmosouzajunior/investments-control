param(
    [string]$ApiUrl = "http://localhost:5241/api"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "backend\src\Omega.Invest.API\Omega.Invest.API.csproj"
$apiOutput = Join-Path $repoRoot "artifacts\api-prd-local"
$frontendRoot = Join-Path $repoRoot "frontend"
$frontendConfig = Join-Path $frontendRoot "dist\frontend\browser\app-config.json"

dotnet publish $apiProject -c Release -o $apiOutput
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Push-Location $frontendRoot
try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "npm run build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $frontendConfig)) {
    throw "Frontend app-config artifact was not found: $frontendConfig"
}

Set-Content -LiteralPath $frontendConfig -Value "{`n  `"apiUrl`": `"$ApiUrl`"`n}" -Encoding UTF8

Write-Host "Local production build completed."
Write-Host "API artifact: $apiOutput"
Write-Host "Frontend API URL: $ApiUrl"
