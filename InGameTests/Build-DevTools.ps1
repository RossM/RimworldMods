#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RimWorldDirectory,
    [Parameter(Mandatory)] [string] $ModDirectory,
    [string] $Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$revision = '0600eb11a6781371ee84a5d9a4ce86653c92d7ff'
$cache = Join-Path $PSScriptRoot "../temp/Yobai.TestDependencies/DevTools-$revision"
if (!(Test-Path -LiteralPath (Join-Path $cache '.complete'))) {
    New-Item -ItemType Directory -Path $cache -Force | Out-Null
    $uri = "https://codeload.github.com/SmashPhil/DevTools/zip/$revision"
    Write-Host "Downloading DevTools $revision..."
    Invoke-WebRequest -Uri $uri -OutFile "$cache.zip" -UseBasicParsing
    Expand-Archive -LiteralPath "$cache.zip" -DestinationPath $cache -Force
    [IO.File]::WriteAllText((Join-Path $cache '.complete'), $uri)
}

$project = Join-Path $cache "DevTools-$revision/DevTools/DevTools.csproj"
& dotnet build $project --configuration $Configuration "-p:RefDir=$RimWorldDirectory" "-p:ModDir=$ModDirectory" '-p:TreatWarningsAsErrors=false' --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'DevTools build failed.' }

New-Item -ItemType Directory -Path (Join-Path $ModDirectory 'About') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DevTools/About.xml') -Destination (Join-Path $ModDirectory 'About/About.xml')
