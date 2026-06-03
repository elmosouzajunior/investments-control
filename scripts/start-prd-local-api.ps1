param(
    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$Database = "OmegaInvestPrdLocal",
    [string]$Url = "http://localhost:5241",
    [string]$JwtKey = $env:OMEGA_INVEST_PRD_LOCAL_JWT_KEY
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "backend\src\Omega.Invest.API\Omega.Invest.API.csproj"
$jwtKeyValue = $JwtKey

if ([string]::IsNullOrWhiteSpace($jwtKeyValue)) {
    $jwtKeyBytes = New-Object byte[] 64
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($jwtKeyBytes)
    $jwtKeyValue = [Convert]::ToBase64String($jwtKeyBytes)
}

$env:ASPNETCORE_ENVIRONMENT = "ProductionLocal"
$env:ConnectionStrings__DefaultConnection = "Server=$SqlServer;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
$env:Jwt__Key = $jwtKeyValue

Write-Host "Starting Omega Invest API in ProductionLocal"
Write-Host "Database: $SqlServer / $Database"
Write-Host "URL: $Url"

dotnet run --no-launch-profile --project $apiProject --urls $Url
