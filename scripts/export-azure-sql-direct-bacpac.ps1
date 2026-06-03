param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,

    [Parameter(Mandatory = $true)]
    [string]$UserName,

    [string]$OutputPath = ".\artifacts\omega-invest-prd.bacpac"
)

$ErrorActionPreference = "Stop"

$resolver = Join-Path $PSScriptRoot "resolve-sqlpackage.ps1"
. $resolver

$sqlPackagePath = Resolve-SqlPackage
$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutput

if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$password = Read-Host "Azure SQL password" -AsSecureString
$credential = [System.Net.NetworkCredential]::new("", $password)
$plainPassword = $credential.Password

try {
    $connectionString = "Server=tcp:$ServerName,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$UserName;Password=$plainPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

    Write-Host "Exporting Azure SQL database directly to local BACPAC"
    Write-Host "Server: $ServerName"
    Write-Host "Database: $DatabaseName"
    Write-Host "Output: $resolvedOutput"
    Write-Host "SqlPackage: $sqlPackagePath"

    & $sqlPackagePath `
        /Action:Export `
        /SourceConnectionString:$connectionString `
        /TargetFile:$resolvedOutput

    if ($LASTEXITCODE -ne 0) {
        throw "SqlPackage export failed with exit code $LASTEXITCODE."
    }

    $bacpac = Get-Item -LiteralPath $resolvedOutput
    if ($bacpac.Length -lt 1024) {
        throw "Export finished, but the BACPAC is too small to be valid: $($bacpac.Length) bytes."
    }

    Write-Host "BACPAC exported successfully: $($bacpac.FullName) ($($bacpac.Length) bytes)"
}
finally {
    $plainPassword = $null
    $connectionString = $null
}
