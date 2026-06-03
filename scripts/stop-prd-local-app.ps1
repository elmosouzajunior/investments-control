$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$runRoot = Join-Path $repoRoot "artifacts\prd-local"
$pidFiles = @(
    Join-Path $runRoot "frontend.pid"
    Join-Path $runRoot "api.pid"
)

function Stop-ProcessTree {
    param([int]$ProcessId)

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) {
        return
    }

    $taskkill = Get-Command taskkill.exe -ErrorAction SilentlyContinue
    if ($taskkill) {
        & $taskkill.Source /PID $ProcessId /T /F | Out-Host
        return
    }

    Stop-Process -Id $process.Id -Force
    Write-Host "Stopped process $($process.Id) ($($process.ProcessName))."
}

foreach ($pidFile in $pidFiles) {
    if (-not (Test-Path -LiteralPath $pidFile)) {
        continue
    }

    $processId = Get-Content -LiteralPath $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $processId) {
        Remove-Item -LiteralPath $pidFile -Force
        continue
    }

    Stop-ProcessTree -ProcessId ([int]$processId)

    Remove-Item -LiteralPath $pidFile -Force
}

Write-Host "Omega Invest PRD Local stopped."
