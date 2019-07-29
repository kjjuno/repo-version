#!/usr/bin/env pwsh

$repoVersion = (dotnet run | ConvertFrom-Json)

$fullSemVer = $repoVersion.FullSemVer

Write-Host "FullSemVer: $fullSemVer"
$env:VERSION = $repoVersion.FullSemVer

dotnet pack -c Release
