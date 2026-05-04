#Requires -Version 5.1
<#
.SYNOPSIS
  Downloads the ZimMarket OpenAPI document (Development only) for import into Postman, Bruno, Insomnia, etc.

.DESCRIPTION
  The API must be running with ASPNETCORE_ENVIRONMENT=Development (see src/ZimMarket.API/Program.cs).
  Default base URL matches Properties/launchSettings.json (http profile).

.EXAMPLE
  ./scripts/export-openapi.ps1
.EXAMPLE
  ./scripts/export-openapi.ps1 -BaseUrl "https://localhost:7047" -OutFile "./openapi-v1.json"
#>
[CmdletBinding()]
param(
    [string] $BaseUrl = "http://localhost:5256",
    [string] $OutFile = ""
)

if (-not $OutFile) {
    $repoRoot = if ($PSScriptRoot) {
        (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    } else {
        (Get-Location).Path
    }
    $OutFile = Join-Path $repoRoot "artifacts\openapi-v1.json"
}

$ErrorActionPreference = "Stop"
$uri = "$($BaseUrl.TrimEnd('/'))/openapi/v1.json"

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

try {
    Invoke-WebRequest -Uri $uri -OutFile $OutFile -UseBasicParsing
} catch {
    Write-Error @"
Failed to download OpenAPI from $uri
Start the API first, e.g.:
  dotnet run --project src/ZimMarket.API/ZimMarket.API.csproj
If you use another port or HTTPS, pass -BaseUrl.
"@
    throw
}

Write-Host "Wrote $OutFile"
Write-Host "Postman: Import -> choose file or paste link $uri"
Write-Host "Bruno:   Create/Open collection -> Import -> OpenAPI v3 (file or URL)"
