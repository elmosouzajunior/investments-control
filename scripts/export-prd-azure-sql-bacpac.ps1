param(
    [Parameter(Mandatory = $true)]
    [string]$SourceConnectionString,

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

Write-Host "Exporting Azure SQL PRD database to BACPAC"
Write-Host "Output: $resolvedOutput"

& $sqlPackagePath `
    /Action:Export `
    /SourceConnectionString:$SourceConnectionString `
    /TargetFile:$resolvedOutput

if ($LASTEXITCODE -ne 0) {
    throw "SqlPackage export failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $resolvedOutput)) {
    throw "BACPAC file was not created: $resolvedOutput"
}

$bacpac = Get-Item -LiteralPath $resolvedOutput
if ($bacpac.Length -lt 1024) {
    throw "Export finished, but the BACPAC is too small to be valid: $($bacpac.Length) bytes."
}
