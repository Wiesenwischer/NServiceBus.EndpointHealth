<#
.SYNOPSIS
    Builds and publishes NuGet packages to the on-premise Azure DevOps feed.

.PARAMETER Source
    The NuGet source to push to. Default: ams-alpha.Feed

.EXAMPLE
    .\publish-local.ps1
#>

param(
    [string]$Source = "ams-alpha.Feed"
)

$ErrorActionPreference = "Stop"

Write-Host "Installing/updating GitVersion tool..." -ForegroundColor Cyan
dotnet tool install --global GitVersion.Tool --version 5.* 2>$null
if ($LASTEXITCODE -ne 0) {
    dotnet tool update --global GitVersion.Tool --version 5.*
}

Write-Host "Calculating version with GitVersion..." -ForegroundColor Cyan
$versionJson = dotnet-gitversion /output json | ConvertFrom-Json
$version = $versionJson.SemVer  # SemVer 2.0 format: 0.1.0-ci.10 (with dot)
$assemblyVersion = $versionJson.AssemblySemVer
$fileVersion = $versionJson.AssemblySemFileVer
$infoVersion = $versionJson.InformationalVersion

Write-Host "Version: $version" -ForegroundColor Green
Write-Host "Assembly Version: $assemblyVersion" -ForegroundColor Green

Write-Host "Cleaning artifacts folder..." -ForegroundColor Cyan
if (Test-Path ./artifacts) {
    Remove-Item ./artifacts -Recurse -Force
}

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build --configuration Release `
    /p:Version=$version `
    /p:AssemblyVersion=$assemblyVersion `
    /p:FileVersion=$fileVersion `
    /p:InformationalVersion=$infoVersion

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Packing NuGet packages..." -ForegroundColor Cyan
dotnet pack --configuration Release --no-build --output ./artifacts `
    /p:PackageVersion=$version

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

Write-Host "Done! Published version $version to $Source" -ForegroundColor Green
