param(
    [Parameter(Mandatory = $true)]
    [string]$BacpacPath,

    [string]$TargetServerName = ".\SQLEXPRESS",
    [string]$TargetDatabaseName = "OmegaInvestPrdLocal"
)

$ErrorActionPreference = "Stop"

$resolver = Join-Path $PSScriptRoot "resolve-sqlpackage.ps1"
. $resolver

if (-not (Test-Path -LiteralPath $BacpacPath)) {
    throw "BACPAC file not found: $BacpacPath"
}

$bacpac = Get-Item -LiteralPath $BacpacPath
if ($bacpac.Length -lt 1024) {
    throw "BACPAC file is too small to be valid: $($bacpac.FullName) ($($bacpac.Length) bytes). Export or download it again."
}

$sqlPackagePath = Resolve-SqlPackage

Write-Host "Importing PRD BACPAC into local SQL Server"
Write-Host "Source: $BacpacPath"
Write-Host "Target: $TargetServerName / $TargetDatabaseName"
Write-Host "SqlPackage: $sqlPackagePath"

& $sqlPackagePath `
    /Action:Import `
    /SourceFile:$BacpacPath `
    /TargetServerName:$TargetServerName `
    /TargetDatabaseName:$TargetDatabaseName `
    /TargetTrustServerCertificate:True
