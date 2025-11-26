<#
.SYNOPSIS
    Builds and publishes NuGet packages to the on-premise Azure DevOps feed.

.PARAMETER Source
    The NuGet source to push to. Default: ams-alpha.Feed

.PARAMETER Version
    Version to use. Default: auto-detect from git tag or use 0.0.0-local

.EXAMPLE
    .\publish-local.ps1

.EXAMPLE
    .\publish-local.ps1 -Version 1.0.0-beta.1
#>

param(
    [string]$Source = "ams-alpha.Feed",
    [string]$Version
)

$ErrorActionPreference = "Stop"

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

Write-Host "Cleaning artifacts folder..." -ForegroundColor Cyan
if (Test-Path ./artifacts) {
    Remove-Item ./artifacts -Recurse -Force
}

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build --configuration Release `
    /p:Version=$Version `
    /p:AssemblyVersion=$assemblyVersion `
    /p:FileVersion=$assemblyVersion `
    /p:InformationalVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Packing NuGet packages..." -ForegroundColor Cyan
dotnet pack --configuration Release --no-build --output ./artifacts `
    /p:PackageVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Pushing packages to $Source..." -ForegroundColor Cyan
Get-ChildItem ./artifacts/*.nupkg | ForEach-Object {
    Write-Host "  Pushing $($_.Name)..." -ForegroundColor Yellow
    dotnet nuget push $_.FullName --source $Source --api-key az --skip-duplicate
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Push failed for $($_.Name)!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Done! Published version $Version to $Source" -ForegroundColor Green
