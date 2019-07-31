#!/usr/bin/env pwsh

Param(
    [switch] $Tag
)

$repoVersion = (dotnet run | ConvertFrom-Json)

$semVer = $repoVersion.SemVer;

if ($Tag) {
    git tag $semVer
}

Write-Host "SemVer: $semVer"
$env:VERSION = $semVer

dotnet pack -c Release
