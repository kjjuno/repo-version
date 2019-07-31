#!/usr/bin/env pwsh

Param(
    [switch] $ApplyTag
)

$repoVersion = (dotnet run | ConvertFrom-Json)

$semVer = $repoVersion.SemVer;

if ($ApplyTag) {
    git tag $semVer
}

Write-Host "SemVer: $semVer"
$env:VERSION = $semVer

dotnet pack -c Release
