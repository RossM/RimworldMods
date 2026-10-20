#requires -Version 7.0

<#
.SYNOPSIS
Builds and runs Yobai's in-game tests using a configured RimWorld test installation.
.EXAMPLE
.\Run-Tests.ps1 -RimWorldPath C:\Games\RimWorld\RimWorld.exe -DevToolsAssemblyPath C:\Mods\DevTools\1.7\Assemblies\DevTools.dll
.EXAMPLE
.\Run-Tests.ps1 -RimWorldPath C:\Games\RimWorld\RimWorld.exe -DevToolsAssemblyPath C:\Mods\DevTools\1.7\Assemblies\DevTools.dll -CoreOnly -Filter 'method == IncomingOwnerKeepsAssignedSleepingSlot'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RimWorldPath,
    [string] $DevToolsAssemblyPath,
    [string] $SaveDataDirectory = (Join-Path $PSScriptRoot '../temp/YobaiSaveData'),
    [string] $OutputDirectory = (Join-Path $PSScriptRoot "../TestResults/Yobai/$(Get-Date -Format 'yyyyMMdd-HHmmss')-$([guid]::NewGuid().ToString('N').Substring(0, 8))"),
    [string] $Filter,
    [switch] $CoreOnly,
    [switch] $SkipBuild,
    [ValidateRange(1, 120)] [int] $TimeoutMinutes = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).ProviderPath
if (!$DevToolsAssemblyPath) {
    $configuredPath = & dotnet msbuild (Join-Path $PSScriptRoot 'Yobai.Tests.csproj') -nologo -getProperty:DevToolsAssemblyPath
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not read DevToolsAssemblyPath from the project.'
    }
    $DevToolsAssemblyPath = ($configuredPath -join "`n").Trim()
    if (!$DevToolsAssemblyPath) {
        throw 'Set DevToolsAssemblyPath in Directory.Build.local.props, or supply -DevToolsAssemblyPath. See Yobai.Tests/README.md.'
    }
}
$DevToolsAssemblyPath = (Resolve-Path -LiteralPath $DevToolsAssemblyPath).ProviderPath
$SaveDataDirectory = (Resolve-Path -LiteralPath $SaveDataDirectory).ProviderPath
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$repositoryDirectory = Split-Path -Parent $PSScriptRoot
$modsConfigPath = Join-Path $SaveDataDirectory 'Config/ModsConfig.xml'
if (!(Test-Path -LiteralPath $modsConfigPath -PathType Leaf)) {
    throw "Missing $modsConfigPath. Configure the isolated test installation as described in Yobai.Tests/README.md."
}

[xml] $modsConfig = Get-Content -LiteralPath $modsConfigPath -Raw
foreach ($packageId in 'brrainz.harmony', 'ludeon.rimworld', 'xylthixlm.yobai', 'smashphil.devtools', 'xylthixlm.yobai.tests') {
    if ($packageId -notin @($modsConfig.ModsConfigData.activeMods.li)) {
        throw "Enable $packageId in $modsConfigPath before running the tests."
    }
}

if (!$SkipBuild) {
    & dotnet build (Join-Path $PSScriptRoot 'Yobai.Tests.csproj') "-p:DevToolsAssemblyPath=$DevToolsAssemblyPath" --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Building Yobai.Tests failed with exit code $LASTEXITCODE."
    }
}

$testConfigPath = Join-Path $repositoryDirectory 'ModBin/Yobai.Tests/Configs/UnitTestConfig.xml'
[byte[]] $originalTestConfig = $null
if (Test-Path -LiteralPath $testConfigPath) {
    $originalTestConfig = [IO.File]::ReadAllBytes($testConfigPath)
}
$originalModsConfig = [IO.File]::ReadAllBytes($modsConfigPath)
$process = $null
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $testConfigPath) -Force | Out-Null

try {
    $escapedOutputDirectory = [Security.SecurityElement]::Escape($OutputDirectory)
    [IO.File]::WriteAllText($testConfigPath, "<TestConfig><randSeed>1234567</randSeed><log><folder>$escapedOutputDirectory</folder></log></TestConfig>`r`n")
    if ($CoreOnly) {
        foreach ($dlc in @($modsConfig.SelectNodes('/ModsConfigData/activeMods/li[starts-with(text(), "ludeon.rimworld.")]'))) {
            [void] $dlc.ParentNode.RemoveChild($dlc)
        }
        # RimWorld enables newly discovered expansions unless they are already known.
        $knownExpansions = $modsConfig.SelectSingleNode('/ModsConfigData/knownExpansions')
        if ($null -eq $knownExpansions) {
            $knownExpansions = $modsConfig.CreateElement('knownExpansions')
            [void] $modsConfig.DocumentElement.AppendChild($knownExpansions)
        }
        $dataDirectory = Join-Path (Split-Path -Parent $RimWorldPath) 'Data'
        foreach ($directory in Get-ChildItem -LiteralPath $dataDirectory -Directory) {
            $aboutPath = Join-Path $directory.FullName 'About/About.xml'
            if (!(Test-Path -LiteralPath $aboutPath -PathType Leaf)) {
                continue
            }
            [xml] $about = Get-Content -LiteralPath $aboutPath -Raw
            $packageId = $about.ModMetaData.packageId.ToLowerInvariant()
            if ($packageId.StartsWith('ludeon.rimworld.') -and
                $packageId -notin @($knownExpansions.SelectNodes('li') | ForEach-Object { $_.InnerText })) {
                $entry = $modsConfig.CreateElement('li')
                $entry.InnerText = $packageId
                [void] $knownExpansions.AppendChild($entry)
            }
        }
        $modsConfig.Save($modsConfigPath)
    }

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $RimWorldPath
    $startInfo.WorkingDirectory = Split-Path -Parent $RimWorldPath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    # ArgumentList preserves paths and filter expressions containing spaces without shell quoting.
    foreach ($argument in '-NoWindow', "-savedatafolder=$SaveDataDirectory", '--pid', 'xylthixlm.yobai.tests', '--test', '--exit') {
        $startInfo.ArgumentList.Add($argument)
    }
    if ($Filter) {
        $startInfo.ArgumentList.Add('--where')
        $startInfo.ArgumentList.Add($Filter)
    }

    Write-Host "Running Yobai tests. Logs: $OutputDirectory"
    $process = [Diagnostics.Process]::Start($startInfo)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $timedOut = !$process.WaitForExit($TimeoutMinutes * 60 * 1000)
    if ($timedOut) {
        $process.Kill($true)
        $process.WaitForExit()
    }
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'stdout.log'), $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'stderr.log'), $stderr.GetAwaiter().GetResult())
    if ($timedOut) {
        throw "RimWorld did not finish within $TimeoutMinutes minutes. See $OutputDirectory."
    }
    if ($process.ExitCode -ne 0) {
        throw "RimWorld exited with code $($process.ExitCode). See $OutputDirectory."
    }

    $reportPath = Join-Path $OutputDirectory 'Test.log'
    if (!(Test-Path -LiteralPath $reportPath -PathType Leaf)) {
        throw "DevTools did not write $reportPath. Link ModBin/Yobai.Tests into the test installation's Mods directory."
    }
    $report = Get-Content -LiteralPath $reportPath -Raw
    $summary = [regex]::Match($report, 'Tests finished in \d+ ms with 0 Failed, [1-9]\d* Passed')
    if (!$summary.Success -or $report -notmatch 'Test runner finished\. Result: Passed') {
        throw "The run did not finish with passing tests. Check $reportPath and the test filter."
    }
    Write-Host $summary.Value
    Write-Host "Report: $reportPath"
}
finally {
    if ($null -ne $process) {
        if (!$process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $process.Dispose()
    }
    [IO.File]::WriteAllBytes($modsConfigPath, $originalModsConfig)
    if ($null -ne $originalTestConfig) {
        [IO.File]::WriteAllBytes($testConfigPath, $originalTestConfig)
    }
    elseif (Test-Path -LiteralPath $testConfigPath) {
        Remove-Item -LiteralPath $testConfigPath
    }
}
