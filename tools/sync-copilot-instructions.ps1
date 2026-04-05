param(
    [string]$SourceRoot = "$PSScriptRoot\.."
)

$repoRoot = (Resolve-Path $SourceRoot).Path
$rootGithub = Join-Path $repoRoot ".github"
$solutionGithub = Join-Path $repoRoot "MarketMonitor\.github"

$files = @(
    "copilot-instructions.md",
    "copilot-instructions-ja.md"
)

if (-not (Test-Path $rootGithub)) {
    throw "Root instructions folder was not found: $rootGithub"
}

if (-not (Test-Path $solutionGithub)) {
    New-Item -ItemType Directory -Path $solutionGithub | Out-Null
}

foreach ($file in $files) {
    $source = Join-Path $rootGithub $file
    $target = Join-Path $solutionGithub $file

    if (-not (Test-Path $source)) {
        throw "Source instruction file was not found: $source"
    }

    Copy-Item -Path $source -Destination $target -Force
    Write-Host "Synced: $file"
}

Write-Host "Instruction sync completed."
