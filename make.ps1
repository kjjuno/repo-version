#!/usr/bin/env pwsh

$repoVersion = (dotnet run | ConvertFrom-Json)

$env:VERSION = $repoVersion.FullSemVer

dotnet pack -c Release