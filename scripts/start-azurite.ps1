# Script to start Azurite storage emulator if not already running
param(
    [string]$WorkspaceFolder = (Get-Location).Path
)

$azuriteLocation = Join-Path $WorkspaceFolder ".azurite"
$debugLog = Join-Path $azuriteLocation "debug.log"

# Ensure .azurite directory exists
if (-not (Test-Path $azuriteLocation)) {
    New-Item -ItemType Directory -Path $azuriteLocation -Force | Out-Null
    Write-Host "Created .azurite directory"
}

# Check if Azurite is already running by checking if the port is in use
try {
    $connection = Test-NetConnection -ComputerName localhost -Port 10000 -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
    if ($connection.TcpTestSucceeded) {
        Write-Host "Azurite is already running on port 10000"
        exit 0
    }
} catch {
    # Port test failed, which means Azurite is not running
}

Write-Host "Starting Azurite storage emulator..."

# Start Azurite in the background
$processArgs = @(
    "azurite@latest",
    "--silent", 
    "--location", $azuriteLocation,
    "--debug", $debugLog,
    "--skipApiVersionCheck"
)

try {
    $process = Start-Process -FilePath "npx" -ArgumentList $processArgs -WindowStyle Hidden -PassThru
    Write-Host "Azurite started with PID: $($process.Id)"
    
    # Wait a moment and verify it started
    Start-Sleep -Seconds 2
    
    $connection = Test-NetConnection -ComputerName localhost -Port 10000 -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
    if ($connection.TcpTestSucceeded) {
        Write-Host "Azurite is now running and ready"
    } else {
        Write-Warning "Azurite may not have started properly"
    }
} catch {
    Write-Error "Failed to start Azurite: $($_.Exception.Message)"
    exit 1
}
