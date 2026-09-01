[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('Clr', 'Mono')]
    [string] $Runtime = 'Mono',

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [string] $MonoExecutable,

    [switch] $Profile,

    [string] $MonoProfile,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $NUnitArguments = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-MonoExecutable([string] $RequestedExecutable)
{
    if ($RequestedExecutable)
    {
        if (-not (Test-Path -LiteralPath $RequestedExecutable -PathType Leaf))
        {
            throw "The requested Mono executable does not exist: $RequestedExecutable"
        }

        return (Resolve-Path -LiteralPath $RequestedExecutable).Path
    }

    if ($env:MONO_EXE)
    {
        if (-not (Test-Path -LiteralPath $env:MONO_EXE -PathType Leaf))
        {
            throw "MONO_EXE does not identify an existing file: $env:MONO_EXE"
        }

        return (Resolve-Path -LiteralPath $env:MONO_EXE).Path
    }

    $command = Get-Command mono -CommandType Application -ErrorAction SilentlyContinue
    if ($command)
    {
        return $command.Source
    }

    $candidates = @(
        (Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'Mono\bin\mono.exe'),
        (Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Mono\bin\mono.exe')
    )
    foreach ($candidate in $candidates)
    {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf))
        {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw 'Mono was not found. Install Mono or specify -MonoExecutable or MONO_EXE.'
}

if (($Profile -or $MonoProfile) -and $Runtime -ne 'Mono')
{
    throw 'Mono profiling options can only be used with -Runtime Mono.'
}

if ($Profile -and $MonoProfile)
{
    throw 'Use either -Profile or -MonoProfile, not both.'
}

$testProjectRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $testProjectRoot
$testProject = Join-Path $testProjectRoot 'Source_Disharmony.Tests.csproj'
$testExecutable = Join-Path $testProjectRoot "bin\$Configuration\net4.7.2\Source_Disharmony.Tests.exe"
$dotnet = Get-Command dotnet -CommandType Application -ErrorAction Stop
$runnerExitCode = 1
$expectedProfilePath = $null
$resultDirectory = Join-Path $repositoryRoot 'TestResults\Disharmony'
$effectiveNUnitArguments = [Collections.Generic.List[string]]::new()
foreach ($argument in $NUnitArguments)
{
    $effectiveNUnitArguments.Add($argument)
}

if (-not ($NUnitArguments | Where-Object { $_ -match '^--result(?:=|$)' }))
{
    New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
    $resultPath = Join-Path $resultDirectory "test-result-$($Runtime.ToLowerInvariant()).xml"
    $effectiveNUnitArguments.Add("--result=$resultPath")
}

Push-Location $repositoryRoot
try
{
    Write-Host "Building Disharmony tests ($Configuration)..."
    & $dotnet.Source build $testProject --configuration $Configuration -p:DeployToMods=false
    if ($LASTEXITCODE -ne 0)
    {
        throw "Building Disharmony tests failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $testExecutable -PathType Leaf))
    {
        throw "The test executable was not produced at the expected path: $testExecutable"
    }

    if ($Runtime -eq 'Clr')
    {
        Write-Host 'Running Disharmony tests on the Microsoft CLR...'
        & $testExecutable @effectiveNUnitArguments
        $runnerExitCode = $LASTEXITCODE
    }
    else
    {
        $mono = Resolve-MonoExecutable $MonoExecutable
        $monoArguments = [Collections.Generic.List[string]]::new()

        if ($Profile -or $MonoProfile)
        {
            New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
            $profilePath = Join-Path $resultDirectory ("mono-profile-{0:yyyyMMdd-HHmmss}.mlpd" -f [DateTime]::UtcNow)
            $profilePath = $profilePath.Replace('\', '/')

            $profileSpecification = if ($MonoProfile) { $MonoProfile } else { 'log:sample' }
            if ($profileSpecification -notmatch '(^|,)output=')
            {
                $profileSpecification += ",output=$profilePath"
                $expectedProfilePath = $profilePath
                Write-Host "Requesting Mono profile output at $profilePath"
            }
            else
            {
                Write-Host "Using Mono profiler specification: $profileSpecification"
            }

            $monoArguments.Add("--profile=$profileSpecification")
        }

        $monoArguments.Add($testExecutable)
        foreach ($argument in $effectiveNUnitArguments)
        {
            $monoArguments.Add($argument)
        }

        Write-Host "Running Disharmony tests on Mono: $mono"
        & $mono @monoArguments
        $runnerExitCode = $LASTEXITCODE

        if ($runnerExitCode -eq 0 -and $expectedProfilePath -and -not (Test-Path -LiteralPath $expectedProfilePath -PathType Leaf))
        {
            throw "Mono completed the tests but did not create $expectedProfilePath. The selected Mono distribution may not include the requested profiler module."
        }
    }
}
finally
{
    Pop-Location
}

exit $runnerExitCode
