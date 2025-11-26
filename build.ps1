<#
.SYNOPSIS
    Local build script.
.DESCRIPTION
    Builds the solution with version information from git tags or a specified version.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release
.PARAMETER Version
    Version to use. Default: auto-detect from git tag or use 0.0.0-local
.PARAMETER Pack
    Create NuGet packages after build.
.PARAMETER Test
    Run unit tests after build.
.EXAMPLE
    .\build.ps1
.EXAMPLE
    .\build.ps1 -Configuration Debug -Test
.EXAMPLE
    .\build.ps1 -Pack -Version 1.0.0
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Version,
    [switch]$Pack,
    [switch]$Test
)

$ErrorActionPreference = 'Stop'

# Determine version
if (-not $Version) {
    # Read version from release-please manifest
    $manifestPath = Join-Path $PSScriptRoot ".release-please-manifest.json"
    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $baseVersion = $manifest.'.'
        # Get commit count for unique build number
        $commitCount = git rev-list HEAD --count
        $Version = "$baseVersion-ci.$commitCount"
        Write-Host "Using version from manifest: $Version" -ForegroundColor Yellow
    } else {
        $Version = "0.0.0-ci"
        Write-Host "No manifest found, using: $Version" -ForegroundColor Yellow
    }
} else {
    Write-Host "Using specified version: $Version" -ForegroundColor Green
}

# Calculate assembly version (major.minor.patch.0)
$assemblyVersion = ($Version -replace '-.*$', '') + ".0"
if ($assemblyVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    $assemblyVersion = "0.0.0.0"
}

Write-Host "Version: $Version" -ForegroundColor Green
Write-Host "Assembly Version: $assemblyVersion" -ForegroundColor Gray

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build --configuration $Configuration `
    /p:Version=$Version `
    /p:AssemblyVersion=$assemblyVersion `
    /p:FileVersion=$assemblyVersion `
    /p:InformationalVersion=$Version

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Test) {
    Write-Host "Running unit tests..." -ForegroundColor Cyan
    dotnet test --no-build --configuration $Configuration --filter "FullyQualifiedName!~IntegrationTests"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($Pack) {
    Write-Host "Creating NuGet packages..." -ForegroundColor Cyan
    if (-not (Test-Path ./artifacts)) {
        New-Item -ItemType Directory -Path ./artifacts | Out-Null
    }
    dotnet pack --no-build --configuration $Configuration /p:PackageVersion=$Version --output ./artifacts
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Packages created in ./artifacts" -ForegroundColor Green
    Get-ChildItem ./artifacts/*.nupkg | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
}

Write-Host "Build completed successfully!" -ForegroundColor Green
