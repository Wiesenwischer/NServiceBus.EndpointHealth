<#
.SYNOPSIS
    Local build script with GitVersion support.
.DESCRIPTION
    Builds the solution with version information from GitVersion.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release
.PARAMETER Pack
    Create NuGet packages after build.
.PARAMETER Test
    Run unit tests after build.
.EXAMPLE
    .\build.ps1
.EXAMPLE
    .\build.ps1 -Configuration Debug -Test
.EXAMPLE
    .\build.ps1 -Pack
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Pack,
    [switch]$Test
)

$ErrorActionPreference = 'Stop'

Write-Host "Restoring tools..." -ForegroundColor Cyan
dotnet tool restore

Write-Host "Determining version..." -ForegroundColor Cyan
$semVer = dotnet gitversion /showvariable SemVer
$assemblySemVer = dotnet gitversion /showvariable AssemblySemVer
$assemblySemFileVer = dotnet gitversion /showvariable AssemblySemFileVer
$informationalVersion = dotnet gitversion /showvariable InformationalVersion

Write-Host "Version: $semVer" -ForegroundColor Green
Write-Host "Assembly Version: $assemblySemVer" -ForegroundColor Gray
Write-Host "File Version: $assemblySemFileVer" -ForegroundColor Gray

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build --configuration $Configuration `
    /p:Version=$semVer `
    /p:AssemblyVersion=$assemblySemVer `
    /p:FileVersion=$assemblySemFileVer `
    /p:InformationalVersion="$informationalVersion"

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Test) {
    Write-Host "Running unit tests..." -ForegroundColor Cyan
    dotnet test --no-build --configuration $Configuration --filter "FullyQualifiedName!~IntegrationTests"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($Pack) {
    Write-Host "Creating NuGet packages..." -ForegroundColor Cyan
    dotnet pack --no-build --configuration $Configuration /p:PackageVersion=$semVer --output ./artifacts
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Packages created in ./artifacts" -ForegroundColor Green
}

Write-Host "Build completed successfully!" -ForegroundColor Green
