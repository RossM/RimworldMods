[CmdletBinding()]
param(
    [ValidateSet('Dotnet', 'Mono')] [string] $Runtime = 'Dotnet',
    [ValidateSet('baseline', 'named', 'parsed', 'cached', 'indexed', 'streamlined', 'lazy', 'exact', 'members', 'hybrid', 'current')]
    [string[]] $Variants = @('baseline', 'named', 'parsed', 'members', 'cached', 'indexed', 'streamlined', 'lazy', 'exact', 'hybrid'),
    [ValidateRange(0, 1000)] [int] $AdditionalAssemblies = 0,
    [string] $MonoExecutable = 'C:/Program Files/Mono/bin/mono.exe',
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '../../TestResults/ReflectionBenchmarks'),
    [switch] $Verify,
    [switch] $ColdOnly,
    [ValidateRange(1, 100)] [int] $Repetitions = 1,
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$framework = if ($Runtime -eq 'Dotnet') { 'net10.0' } else { 'net472' }
if (-not $NoBuild) {
    dotnet build (Join-Path $PSScriptRoot 'Disharmony.Benchmarks.csproj') -c Release -f $framework -p:DeployToMods=false --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark build failed.' }
}

New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$extension = if ($Runtime -eq 'Dotnet') { 'dll' } else { 'exe' }
$executable = if ($Runtime -eq 'Dotnet') { 'dotnet' } else { $MonoExecutable }
$assembly = Join-Path $PSScriptRoot "bin/Release/$framework/Disharmony.Benchmarks.$extension"
if ($Verify) {
    & $executable $assembly --verify --workers=0 "--result=$(Join-Path $OutputDirectory "$Runtime-tests.xml")"
    if ($LASTEXITCODE -ne 0) { throw 'Compatibility tests failed; see README for the retained Mono baseline limitation.' }
}

$oldTiering = $env:DOTNET_TieredCompilation
try {
    # Avoid tier transitions part-way through short microbenchmarks. This is recorded in each CSV.
    $env:DOTNET_TieredCompilation = '0'
    foreach ($variant in $Variants) {
        for ($run = 1; $run -le $Repetitions; $run++) {
            $arguments = @($assembly, $variant, "$AdditionalAssemblies")
            if ($ColdOnly) { $arguments += '--cold-only' }
            $file = Join-Path $OutputDirectory "$Runtime-$variant-$AdditionalAssemblies-$run.csv"
            & $executable @arguments | Set-Content -LiteralPath $file -Encoding utf8
            if ($LASTEXITCODE -ne 0) { throw "Benchmark failed: $variant" }
            Write-Output "Saved $file"
        }
    }
}
finally {
    $env:DOTNET_TieredCompilation = $oldTiering
}
