param(
    [string]$SourceRoot = "$PSScriptRoot\.."
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path $SourceRoot).Path
$rootGithub = Join-Path $repoRoot ".github"
$solutionGithub = Join-Path $repoRoot "MarketMonitor\.github"

$files = @(
    "copilot-instructions.md"
)

$hasMismatch = $false

foreach ($file in $files) {
    $left = Join-Path $rootGithub $file
    $right = Join-Path $solutionGithub $file

    if (-not (Test-Path $left)) {
        Write-Error "Missing root instruction file: $left"
        $hasMismatch = $true
        continue
    }

    if (-not (Test-Path $right)) {
        Write-Error "Missing solution instruction file: $right"
        $hasMismatch = $true
        continue
    }

    $leftHash = (Get-FileHash -Algorithm SHA256 -Path $left).Hash
    $rightHash = (Get-FileHash -Algorithm SHA256 -Path $right).Hash

    if ($leftHash -ne $rightHash) {
        Write-Host "Mismatch detected: $file"
        Write-Host "  root:     $left"
        Write-Host "  solution: $right"
        $hasMismatch = $true
    }
    else {
        Write-Host "OK: $file"
    }
}

if ($hasMismatch) {
    Write-Host ""
    Write-Host "Instructions are out of sync. Run the command below and commit the result:"
    Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\sync-copilot-instructions.ps1"
    exit 1
}

Write-Host "All instruction files are in sync."
