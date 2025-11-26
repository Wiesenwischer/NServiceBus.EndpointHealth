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
    Write-Host "Determining version from git..." -ForegroundColor Cyan

    # Try to get version from latest tag
    $tag = $null
    $tag = git describe --tags --abbrev=0 2>&1
    if ($LASTEXITCODE -eq 0 -and $tag -match '^v?(\d+\.\d+\.\d+.*)$') {
        $baseVersion = $Matches[1]

        # Check if we're exactly on the tag
        $null = git describe --tags --exact-match 2>&1
        if ($LASTEXITCODE -eq 0) {
            $Version = $baseVersion
            Write-Host "Using version from tag: $Version" -ForegroundColor Green
        } else {
            # We're ahead of the tag, use local suffix
            $commitCount = git rev-list "$tag..HEAD" --count
            $Version = "$baseVersion-local.$commitCount"
            Write-Host "Using version: $Version (based on $tag + $commitCount commits)" -ForegroundColor Yellow
        }
    } else {
        $Version = "0.0.0-local"
        Write-Host "No git tag found, using: $Version" -ForegroundColor Yellow
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
