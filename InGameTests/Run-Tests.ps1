#requires -Version 5.1

<#
.SYNOPSIS
Builds and runs the selected in-game tests against the installed Steam version of RimWorld.
.EXAMPLE
.\Run-Tests.ps1 -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe"
.EXAMPLE
.\Run-Tests.ps1 -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe" -CoreOnly -Filter 'method == IncomingOwnerKeepsAssignedSleepingSlot'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RimWorldPath,
    [ValidateSet('Yobai', 'Xylib', 'XylXenos')] [string] $Suite = 'Yobai',
    [string] $OutputDirectory = (Join-Path $PSScriptRoot "../TestResults/$Suite/$(Get-Date -Format 'yyyyMMdd-HHmmss')-$([guid]::NewGuid().ToString('N').Substring(0, 8))"),
    [string] $Filter,
    [string] $DisharmonySourceDirectory,
    [switch] $CoreOnly,
    [ValidateRange(1, 120)] [int] $TimeoutMinutes = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-DependencyArchive([string] $Name, [string] $Uri) {
    $destination = Join-Path $dependencyDirectory $Name
    $complete = Join-Path $destination '.complete'
    if (!(Test-Path -LiteralPath $complete)) {
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        $archive = "$destination.zip"
        Write-Host "Downloading $Name..."
        Invoke-WebRequest -Uri $Uri -OutFile $archive -UseBasicParsing
        Expand-Archive -LiteralPath $archive -DestinationPath $destination -Force
        [IO.File]::WriteAllText($complete, $Uri)
    }
    return $destination
}

function ConvertTo-ProcessArgument([string] $Argument) {
    # Windows arguments double backslashes before embedded or closing quotes.
    $escaped = [regex]::Replace($Argument, '(\\*)"', '$1$1\"')
    $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}

function Invoke-Build([string[]] $BuildArguments) {
    & dotnet build @BuildArguments --verbosity quiet | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $($BuildArguments[0])"
    }
}

$RimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).ProviderPath
$gameDirectory = Split-Path -Parent $RimWorldPath
$gameVersion = (Get-Content -LiteralPath (Join-Path $gameDirectory 'Version.txt') -Raw -Encoding utf8).Trim()
$repositoryDirectory = Split-Path -Parent $PSScriptRoot
$testProjectDirectory = Join-Path $repositoryDirectory "$Suite.Tests"
$dependencyDirectory = Join-Path $repositoryDirectory 'temp/Yobai.TestDependencies'
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null

if ($CoreOnly -and $Suite -ne 'Yobai') { throw 'Xylib and XylXenos tests require Biotech; omit -CoreOnly.' }

# DevTools is pinned test infrastructure; Disharmony can optionally come from source.
$devToolsRevision = '0600eb11a6781371ee84a5d9a4ce86653c92d7ff'
$devToolsArchive = Get-DependencyArchive "DevTools-$devToolsRevision" "https://codeload.github.com/SmashPhil/DevTools/zip/$devToolsRevision"
$devToolsSource = Join-Path $devToolsArchive "DevTools-$devToolsRevision"
$devToolsMod = Join-Path $dependencyDirectory 'DevToolsMod'
$devToolsAssembly = Join-Path $devToolsMod '1.7/Assemblies/DevTools.dll'

Invoke-Build @(
    (Join-Path $devToolsSource 'DevTools/DevTools.csproj'),
    "-p:RefDir=$gameDirectory", "-p:ModDir=$devToolsMod", '-p:TreatWarningsAsErrors=false'
)
New-Item -ItemType Directory -Path (Join-Path $devToolsMod 'About') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $devToolsMod 'About/About.xml'), '<ModMetaData><packageId>SmashPhil.DevTools</packageId><name>DevTools (in-game tests)</name><author>Smash Phil</author><supportedVersions><li>1.7</li></supportedVersions></ModMetaData>')
$testBuildArguments = @(
    (Join-Path $testProjectDirectory "$Suite.Tests.csproj"), '--configuration', 'Release',
    "-p:DevToolsAssemblyPath=$devToolsAssembly",
    '-p:ImportLocalBuildProps=false'
)
if ($DisharmonySourceDirectory) {
    if ($Suite -eq 'Yobai') { throw 'The local Disharmony option currently supports the Xylib and XylXenos suites.' }
    $DisharmonySourceDirectory = (Resolve-Path -LiteralPath $DisharmonySourceDirectory).ProviderPath
}
$testBuildArguments += "-p:DisharmonySourceDirectory=$DisharmonySourceDirectory"
Invoke-Build $testBuildArguments

$modsDirectory = Join-Path $gameDirectory 'Mods'
$workshopDirectory = [IO.Path]::GetFullPath((Join-Path $gameDirectory '../../workshop/content/294100'))
$workshopPackages = @{}
$workshopIds = @('2009463077')
if (!$DisharmonySourceDirectory) { $workshopIds += '3818084845' }
foreach ($workshopId in $workshopIds) {
    $aboutPath = Join-Path $workshopDirectory "$workshopId/About/About.xml"
    if (!(Test-Path -LiteralPath $aboutPath)) {
        throw "Missing Workshop dependency $workshopId. Install it through Steam before testing."
    }
    [xml] $about = Get-Content -LiteralPath $aboutPath -Raw -Encoding utf8
    $packageId = $about.ModMetaData.packageId.ToLowerInvariant()
    $workshopPackages[$packageId] = $packageId
}
# RimWorld distinguishes Workshop mods from local copies with this suffix.
foreach ($directory in Get-ChildItem -LiteralPath $modsDirectory -Directory) {
    $aboutPath = Join-Path $directory.FullName 'About/About.xml'
    if (Test-Path -LiteralPath $aboutPath) {
        [xml] $about = Get-Content -LiteralPath $aboutPath -Raw -Encoding utf8
        $packageId = $about.ModMetaData.packageId.ToLowerInvariant()
        if ($workshopPackages.ContainsKey($packageId)) {
            $workshopPackages[$packageId] = "${packageId}_steam"
        }
    }
}

$testMod = Join-Path $OutputDirectory "$Suite.Tests"
New-Item -ItemType Directory -Path $testMod | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repositoryDirectory "ModBin/$Suite.Tests") | Copy-Item -Destination $testMod -Recurse
$gameplayMod = if ($Suite -eq 'Yobai') { 'Yobai' } else { 'XylXenos' }
$gameplayPackage = if ($Suite -eq 'Yobai') { 'xylthixlm.yobai' } else { 'xylthixlm.races.core' }
$testPackage = ('xylthixlm.' + $Suite + '.tests').ToLowerInvariant()
$mods = @(
    @{ Name = $gameplayMod; Source = (Join-Path $repositoryDirectory "ModBin/$gameplayMod") },
    @{ Name = 'DevTools'; Source = $devToolsMod },
    @{ Name = "$Suite.Tests"; Source = $testMod }
)
if ($DisharmonySourceDirectory) {
    # Match the runtime to the source used for the build, without changing Workshop files.
    $localDisharmony = Join-Path $OutputDirectory 'Disharmony'
    New-Item -ItemType Directory -Path (Join-Path $localDisharmony 'Assemblies') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $DisharmonySourceDirectory 'Disharmony.RimWorld/About') -Destination $localDisharmony -Recurse
    foreach ($assembly in 'Disharmony.dll', 'Disharmony.RimWorld.dll', '0Harmony.dll') {
        Copy-Item -LiteralPath (Join-Path $repositoryDirectory "XylXenos/bin/Release/net10.0/$assembly") -Destination (Join-Path $localDisharmony 'Assemblies')
    }
    $mods = @(@{ Name = 'Disharmony'; Source = $localDisharmony }) + $mods
    $workshopPackages['xylthixlm.disharmony'] = 'xylthixlm.disharmony'
}

$saveDataDirectory = Join-Path $OutputDirectory 'UserData'
$configDirectory = Join-Path $saveDataDirectory 'Config'
New-Item -ItemType Directory -Path $configDirectory -Force | Out-Null
$expansionMetadata = @()
foreach ($directory in Get-ChildItem -LiteralPath (Join-Path $gameDirectory 'Data') -Directory) {
    $aboutPath = Join-Path $directory.FullName 'About/About.xml'
    if (Test-Path -LiteralPath $aboutPath -PathType Leaf) {
        [xml] $about = Get-Content -LiteralPath $aboutPath -Raw -Encoding utf8
        $package = $about.ModMetaData.packageId.ToLowerInvariant()
        if ($package.StartsWith('ludeon.rimworld.')) {
            $expansionMetadata += $about
        }
    }
}
# Each official expansion lists all earlier expansions in forceLoadAfter.
$expansions = @($expansionMetadata | Sort-Object { $_.SelectNodes('/ModMetaData/forceLoadAfter/li').Count } |
    ForEach-Object { $_.ModMetaData.packageId.ToLowerInvariant() })
$activeMods = @($workshopPackages['brrainz.harmony'], 'ludeon.rimworld')
if (!$CoreOnly) {
    $activeMods += $expansions
}
$activeMods += @($workshopPackages['xylthixlm.disharmony'], $gameplayPackage, 'smashphil.devtools', $testPackage)
$activeXml = ($activeMods | ForEach-Object { "<li>$($_)</li>" }) -join ''
$expansionsXml = ($expansions | ForEach-Object { "<li>$($_)</li>" }) -join ''
[IO.File]::WriteAllText((Join-Path $configDirectory 'ModsConfig.xml'), @"
<ModsConfigData>
  <version>$gameVersion</version>
  <activeMods>$activeXml</activeMods>
  <knownExpansions>$expansionsXml</knownExpansions>
</ModsConfigData>
"@)
[IO.File]::WriteAllText((Join-Path $configDirectory 'Prefs.xml'), '<PrefsData><runInBackground>True</runInBackground></PrefsData>')
$testConfigDirectory = Join-Path $testMod 'Configs'
New-Item -ItemType Directory -Path $testConfigDirectory -Force | Out-Null
$escapedOutputDirectory = [Security.SecurityElement]::Escape($OutputDirectory)
[IO.File]::WriteAllText((Join-Path $testConfigDirectory 'UnitTestConfig.xml'), "<TestConfig><randSeed>1234567</randSeed><log><folder>$escapedOutputDirectory</folder></log></TestConfig>")

$backupDirectory = Join-Path $gameDirectory ("$Suite.Tests." + [guid]::NewGuid().ToString('N'))
$backups = @()
$links = @()
$process = $null
$stdoutFile = $null
$stderrFile = $null
$stdout = $null
$stderr = $null
try {
    foreach ($mod in $mods) {
        $linkPath = [IO.Path]::GetFullPath((Join-Path $modsDirectory $mod.Name))
        $backupPath = [IO.Path]::GetFullPath((Join-Path $backupDirectory $mod.Name))
        if ((Split-Path -Parent $linkPath) -ne $modsDirectory -or
            (Split-Path -Parent $backupPath) -ne $backupDirectory) {
            throw "Test mod path is outside its installation or backup directory: $($mod.Name)"
        }
        if (Test-Path -LiteralPath $linkPath) {
            New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
            [IO.Directory]::Move($linkPath, $backupPath)
            $backups += @{ Original = $linkPath; Backup = $backupPath }
        }
        New-Item -ItemType Junction -Path $linkPath -Target $mod.Source | Out-Null
        $links += @{ Path = $linkPath; Target = $mod.Source }
    }
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $RimWorldPath
    $startInfo.WorkingDirectory = $gameDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $arguments = @('-Headless', "-savedatafolder=$saveDataDirectory", '--pid', $testPackage, '--test', '--exit')
    if ($Filter) {
        $arguments += @('--where', $Filter)
    }
    $startInfo.Arguments = ($arguments | ForEach-Object { ConvertTo-ProcessArgument $_ }) -join ' '
    Write-Host ("Running RimWorld {0}: {1}" -f $gameVersion, $RimWorldPath)
    Write-Host "Logs: $OutputDirectory"
    $process = [Diagnostics.Process]::Start($startInfo)
    $stdoutFile = [IO.File]::Open((Join-Path $OutputDirectory 'stdout.log'), 'Create', 'Write', 'Read')
    $stderrFile = [IO.File]::Open((Join-Path $OutputDirectory 'stderr.log'), 'Create', 'Write', 'Read')
    $stdout = $process.StandardOutput.BaseStream.CopyToAsync($stdoutFile)
    $stderr = $process.StandardError.BaseStream.CopyToAsync($stderrFile)
    if (!$process.WaitForExit($TimeoutMinutes * 60 * 1000)) {
        $process.Kill()
        $process.WaitForExit()
        throw "RimWorld did not finish within $TimeoutMinutes minutes. See $OutputDirectory."
    }
    $null = $stdout.GetAwaiter().GetResult()
    $null = $stderr.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 0) {
        throw "RimWorld exited with code $($process.ExitCode). See $OutputDirectory."
    }
    $reportPath = Join-Path $OutputDirectory 'Test.log'
    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding utf8
    $summary = [regex]::Match($report, 'Tests finished in \d+ ms with 0 Failed, [1-9]\d* Passed')
    if (!$summary.Success -or $report -notmatch 'Test runner finished\. Result: Passed') {
        throw "The run did not finish with passing tests. Check $reportPath and the test filter."
    }
    Write-Host $summary.Value
    Write-Host "Report: $reportPath"
}
finally {
    try {
        if ($null -ne $process -and !$process.HasExited) {
            $process.Kill()
            $process.WaitForExit()
        }
        if ($null -ne $stdout) { $null = $stdout.GetAwaiter().GetResult() }
        if ($null -ne $stderr) { $null = $stderr.GetAwaiter().GetResult() }
    }
    finally {
        if ($null -ne $process) { $process.Dispose() }
        if ($null -ne $stdoutFile) { $stdoutFile.Dispose() }
        if ($null -ne $stderrFile) { $stderrFile.Dispose() }
        foreach ($link in $links) {
            $item = Get-Item -LiteralPath $link.Path -Force
            if ($item.LinkType -ne 'Junction' -or $item.Target -ne $link.Target) {
                throw "Temporary test link changed; leaving it in place: $($link.Path)"
            }
            [IO.Directory]::Delete($link.Path)
        }
        foreach ($backup in $backups) {
            [IO.Directory]::Move($backup.Backup, $backup.Original)
        }
        if (Test-Path -LiteralPath $backupDirectory) {
            [IO.Directory]::Delete($backupDirectory)
        }
    }
}
