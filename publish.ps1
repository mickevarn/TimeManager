Param(
    [string]$Configuration = 'Release',
    [string]$PublishDir = 'Published',
    [string]$ProjectRelative = 'TimeManager\TimeManager.csproj',
    [switch]$Run
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectPath = Join-Path $scriptDir $ProjectRelative
$publishPath = Join-Path $scriptDir $PublishDir

if (-not (Test-Path $projectPath)) {
    Write-Error "Project not found: $projectPath"
    exit 1
}

# Ensure publish folder exists (don't mix outputs from multiple projects)
New-Item -ItemType Directory -Force -Path $publishPath | Out-Null

Write-Host "Publishing project: $projectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Output folder: $publishPath"

dotnet publish $projectPath -c $Configuration -o $publishPath
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed with exit code $LASTEXITCODE"; exit $LASTEXITCODE }

Write-Host "Publish completed. Files are in: $publishPath"

if ($Run) {
    $exePath = Join-Path $publishPath 'TimeManager.exe'
    $dllPath = Join-Path $publishPath 'TimeManager.dll'

    if (Test-Path $exePath) {
        Write-Host "Starting $exePath"
        Start-Process -FilePath $exePath -WorkingDirectory $publishPath
    }
    elseif (Test-Path $dllPath) {
        Write-Host "Starting dotnet $dllPath"
        Start-Process -FilePath 'dotnet' -ArgumentList $dllPath -WorkingDirectory $publishPath
    }
    else {
        Write-Error "No runnable artifact found in publish folder."
    }
}

Write-Host "Done."