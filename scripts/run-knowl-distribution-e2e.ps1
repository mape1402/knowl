param(
    [int]$SqlPort = 14333,
    [int]$ControlPlanePort = 18080,
    [int]$RuntimePort = 18081,
    [string]$SqlPassword = "KnOwl_e2e_Str0ng!2026",
    [switch]$KeepSqlContainer
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$containerName = "knowl-e2e-sql"
$controlDatabase = "KnOwlE2EControl"
$runtimeDatabase = "KnOwlE2ERuntime"
$controlConnection = "Server=127.0.0.1,$SqlPort;Database=$controlDatabase;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True;Encrypt=False"
$runtimeConnection = "Server=127.0.0.1,$SqlPort;Database=$runtimeDatabase;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True;Encrypt=False"
$artifactsPath = Join-Path $repoRoot "artifacts\e2e"
$runtimeLog = Join-Path $artifactsPath "runtime-host.log"
$controlLog = Join-Path $artifactsPath "control-plane.log"

New-Item -ItemType Directory -Force -Path $artifactsPath | Out-Null

function Invoke-Docker {
    param([string[]]$DockerArgs)
    & docker @DockerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed: docker $($DockerArgs -join ' ')"
    }
}

function Wait-HttpOk {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    throw "Timed out waiting for $Url"
}

function Wait-SqlServer {
    param([int]$TimeoutSeconds = 120)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $status = & docker inspect -f "{{.State.Status}}" $containerName 2>$null
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($status)) {
            Start-Sleep -Seconds 1
            continue
        }

        $logs = & docker logs $containerName 2>&1
        if (($logs -join "`n") -match "SQL Server is now ready for client connections") {
            return
        }

        if ($status -eq "exited" -or $status -eq "dead") {
            throw "SQL Server container stopped before readiness. Logs: $($logs -join "`n")"
        }

        Start-Sleep -Seconds 3
    }

    throw "Timed out waiting for SQL Server container readiness."
}

function Start-DotnetHost {
    param(
        [string]$ProjectPath,
        [string]$Urls,
        [hashtable]$Environment,
        [string]$LogPath
    )

    $hostScript = Join-Path $env:TEMP ("knowl-e2e-host-" + [Guid]::NewGuid().ToString("N") + ".ps1")
    $lines = @(
        "`$ErrorActionPreference = 'Stop'",
        "Set-Location '$repoRoot'",
        "`$env:ASPNETCORE_ENVIRONMENT = 'Development'",
        "`$env:ASPNETCORE_URLS = '$Urls'"
    )

    foreach ($entry in $Environment.GetEnumerator()) {
        $escapedValue = ($entry.Value -replace "'", "''")
        $lines += "`$env:$($entry.Key) = '$escapedValue'"
    }

    $lines += "dotnet run --project '$ProjectPath' --no-build --no-launch-profile --framework net10.0"
    Set-Content -Path $hostScript -Value $lines -Encoding UTF8

    Start-Process -FilePath "powershell" `
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$hostScript`"" `
        -WindowStyle Hidden `
        -RedirectStandardOutput $LogPath `
        -RedirectStandardError "$LogPath.err" `
        -PassThru
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    if (-not $Process) {
        return
    }

    $children = Get-CimInstance Win32_Process | Where-Object { $_.ParentProcessId -eq $Process.Id }
    foreach ($child in $children) {
        $childProcess = Get-Process -Id $child.ProcessId -ErrorAction SilentlyContinue
        if ($childProcess) {
            Stop-ProcessTree $childProcess
        }
    }

    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
}

$runtimeProcess = $null
$controlProcess = $null

try {
    Push-Location $repoRoot

    $existingContainer = & docker ps -a --filter "name=^/$containerName$" --format "{{.Names}}"
    if ($existingContainer -eq $containerName) {
        Invoke-Docker @("rm", "-f", $containerName) | Out-Null
    }

    Invoke-Docker @(
        "run",
        "--name", $containerName,
        "-e", "ACCEPT_EULA=Y",
        "-e", "MSSQL_SA_PASSWORD=$SqlPassword",
        "-p", "$SqlPort`:1433",
        "-d", "mcr.microsoft.com/mssql/server:2022-latest"
    ) | Out-Null

    Wait-SqlServer

    dotnet build KnOwl.slnx --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }

    $previousControl = $env:KNOWL_CONTROL_SQL_INTEGRATION_CONNECTION
    $previousRuntime = $env:KNOWL_RUNTIME_SQL_INTEGRATION_CONNECTION
    $previousRuntimeUrl = $env:KNOWL_E2E_RUNTIME_BASE_URL
    $previousControlUrl = $env:KNOWL_E2E_CONTROL_BASE_URL

    try {
        $env:KNOWL_CONTROL_SQL_INTEGRATION_CONNECTION = $controlConnection
        $env:KNOWL_RUNTIME_SQL_INTEGRATION_CONNECTION = $runtimeConnection
        $env:KNOWL_E2E_RUNTIME_BASE_URL = "http://127.0.0.1:$RuntimePort"
        $env:KNOWL_E2E_CONTROL_BASE_URL = "http://127.0.0.1:$ControlPlanePort"

        foreach ($framework in @("net10.0", "net9.0")) {
            $runtimeProcess = Start-DotnetHost `
                -ProjectPath "samples\KnOwl.RuntimeHost.Sample\KnOwl.RuntimeHost.Sample.csproj" `
                -Urls "http://127.0.0.1:$RuntimePort" `
                -Environment @{
                    "ConnectionStrings__KnOwlRuntimeDb" = $runtimeConnection
                    "Database__UseManagedIdentity" = "false"
                } `
                -LogPath $runtimeLog

            $controlProcess = Start-DotnetHost `
                -ProjectPath "samples\KnOwl.ControlPlaneHost.Sample\KnOwl.ControlPlaneHost.Sample.csproj" `
                -Urls "http://127.0.0.1:$ControlPlanePort" `
                -Environment @{
                    "ConnectionStrings__KnOwlDb" = $controlConnection
                    "Database__UseManagedIdentity" = "false"
                } `
                -LogPath $controlLog

            try {
                Wait-HttpOk "http://127.0.0.1:$RuntimePort/health"
                Wait-HttpOk "http://127.0.0.1:$ControlPlanePort/health"

                dotnet test "tests\KnOwl.Tests\KnOwl.Tests.csproj" `
                    --no-build `
                    --framework $framework `
                    --filter "FullyQualifiedName~SqlServerStorageIntegrationTests|FullyQualifiedName~DistributionRuntimeE2ETests" `
                    --logger "console;verbosity=normal"
                if ($LASTEXITCODE -ne 0) {
                    throw "E2E tests failed for $framework."
                }
            }
            finally {
                Stop-ProcessTree $runtimeProcess
                Stop-ProcessTree $controlProcess
                $runtimeProcess = $null
                $controlProcess = $null
            }
        }
    }
    finally {
        $env:KNOWL_CONTROL_SQL_INTEGRATION_CONNECTION = $previousControl
        $env:KNOWL_RUNTIME_SQL_INTEGRATION_CONNECTION = $previousRuntime
        $env:KNOWL_E2E_RUNTIME_BASE_URL = $previousRuntimeUrl
        $env:KNOWL_E2E_CONTROL_BASE_URL = $previousControlUrl
    }
}
finally {
    Stop-ProcessTree $runtimeProcess
    Stop-ProcessTree $controlProcess

    if (-not $KeepSqlContainer) {
        $existingContainer = & docker ps -a --filter "name=^/$containerName$" --format "{{.Names}}"
        if ($existingContainer -eq $containerName) {
            & docker rm -f $containerName | Out-Null
        }
    }

    Pop-Location
}
