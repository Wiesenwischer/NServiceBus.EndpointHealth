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
