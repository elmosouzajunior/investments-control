param(
    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$Database = "OmegaInvestPrdLocal",
    [string]$ApiUrl = "http://localhost:5241",
    [string]$FrontendUrl = "http://localhost:4200",
    [string]$JwtKey = $env:OMEGA_INVEST_PRD_LOCAL_JWT_KEY
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$runRoot = Join-Path $repoRoot "artifacts\prd-local"
$apiProject = Join-Path $repoRoot "backend\src\Omega.Invest.API\Omega.Invest.API.csproj"
$frontendRoot = Join-Path $repoRoot "frontend"
$apiPidFile = Join-Path $runRoot "api.pid"
$frontendPidFile = Join-Path $runRoot "frontend.pid"
$apiOut = Join-Path $runRoot "api.out.log"
$apiErr = Join-Path $runRoot "api.err.log"
$frontendOut = Join-Path $runRoot "frontend.out.log"
$frontendErr = Join-Path $runRoot "frontend.err.log"
$jwtKeyValue = $JwtKey

if ([string]::IsNullOrWhiteSpace($jwtKeyValue)) {
    $jwtKeyBytes = New-Object byte[] 64
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($jwtKeyBytes)
    $jwtKeyValue = [Convert]::ToBase64String($jwtKeyBytes)
}

if (-not (Test-Path -LiteralPath $runRoot)) {
    New-Item -ItemType Directory -Path $runRoot | Out-Null
}

function Test-LocalPort {
    param([int]$Port)

    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    return $null -ne $connection
}

function Start-ProcessWithEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [object]$ArgumentList,

        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory = $true)]
        [string]$RedirectStandardOutput,

        [Parameter(Mandatory = $true)]
        [string]$RedirectStandardError,

        [hashtable]$Environment = @{}
    )

    $previousValues = @{}

    try {
        foreach ($entry in $Environment.GetEnumerator()) {
            $previousValues[$entry.Key] = [Environment]::GetEnvironmentVariable($entry.Key, "Process")
            [Environment]::SetEnvironmentVariable($entry.Key, [string]$entry.Value, "Process")
        }

        return Start-Process $FilePath `
            -ArgumentList $ArgumentList `
            -WorkingDirectory $WorkingDirectory `
            -RedirectStandardOutput $RedirectStandardOutput `
            -RedirectStandardError $RedirectStandardError `
            -PassThru `
            -WindowStyle Hidden
    }
    finally {
        foreach ($entry in $previousValues.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Process")
        }
    }
}

function Wait-LocalPort {
    param(
        [int]$Port,
        [string]$Name,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-LocalPort -Port $Port) {
            Write-Host "$Name is listening on port $Port."
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "$Name did not start listening on port $Port within $TimeoutSeconds seconds."
}

function Start-Api {
    if (Test-LocalPort -Port 5241) {
        Write-Host "API already appears to be running on port 5241."
        return
    }

    $environment = @{
        "ASPNETCORE_ENVIRONMENT" = "ProductionLocal"
        "ConnectionStrings__DefaultConnection" = "Server=$SqlServer;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        "Jwt__Key" = $jwtKeyValue
    }

    $process = Start-ProcessWithEnvironment `
        -FilePath "dotnet" `
        -ArgumentList "run --no-launch-profile --project `"$apiProject`" --urls `"$ApiUrl`"" `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $apiOut `
        -RedirectStandardError $apiErr `
        -Environment $environment

    Set-Content -LiteralPath $apiPidFile -Value $process.Id -Encoding ASCII
    Write-Host "API started. PID: $($process.Id)"
}

function Start-Frontend {
    if (Test-LocalPort -Port 4200) {
        Write-Host "Frontend already appears to be running on port 4200."
        return
    }

    $npm = (Get-Command npm.cmd -ErrorAction SilentlyContinue)
    if (-not $npm) {
        $npm = Get-Command npm -ErrorAction Stop
    }

    $environment = @{
        "CI" = "true"
        "NG_CLI_ANALYTICS" = "false"
    }

    $process = Start-ProcessWithEnvironment `
        -FilePath $npm.Source `
        -ArgumentList @("run", "start", "--", "--host", "127.0.0.1", "--port", "4200") `
        -WorkingDirectory $frontendRoot `
        -RedirectStandardOutput $frontendOut `
        -RedirectStandardError $frontendErr `
        -Environment $environment

    Set-Content -LiteralPath $frontendPidFile -Value $process.Id -Encoding ASCII
    Write-Host "Frontend started. PID: $($process.Id)"
}

Start-Api
Start-Frontend

Write-Host "Waiting for the local app to warm up..."
Wait-LocalPort -Port 5241 -Name "API"
Wait-LocalPort -Port 4200 -Name "Frontend"

Start-Process $FrontendUrl

Write-Host "Omega Invest PRD Local is running."
Write-Host "App: $FrontendUrl"
Write-Host "API: $ApiUrl"
Write-Host "Logs: $runRoot"
