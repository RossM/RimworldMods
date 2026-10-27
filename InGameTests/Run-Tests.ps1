#requires -Version 5.1

<#
.SYNOPSIS
Builds and runs the selected in-game tests against the installed Steam version of RimWorld.
.EXAMPLE
.\Run-Tests.ps1 -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe"
.EXAMPLE
.\Run-Tests.ps1 -Expansions None -Filter 'method == IncomingOwnerKeepsAssignedSleepingSlot'
.EXAMPLE
.\Run-Tests.ps1 -Suite XylXenos -Expansions Royalty,Biotech
#>
[CmdletBinding()]
param(
    [string] $RimWorldPath = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe',
    [ValidateSet('Yobai', 'Xylib', 'XylXenos')] [string] $Suite = 'Yobai',
    [string] $OutputDirectory = (Join-Path $PSScriptRoot "../TestResults/$Suite/$(Get-Date -Format 'yyyyMMdd-HHmmss')-$([guid]::NewGuid().ToString('N').Substring(0, 8))"),
    [string] $Filter,
    [ValidateSet('Debug', 'Release')] [string] $Configuration = 'Debug',
    [switch] $SkipBuild,
    [string] $DisharmonySourceDirectory,
    [ValidateNotNullOrEmpty()] [string[]] $Expansions,
    [ValidateRange(1, 120)] [int] $TimeoutMinutes = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-ModLink([string] $Name, [string] $Source) {
    $linkPath = Join-Path $modsDirectory $Name
    $sourcePath = (Resolve-Path -LiteralPath $Source).ProviderPath
    $existing = Get-Item -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue
    if ($null -ne $existing) {
        if ($existing.LinkType -in @('Junction', 'SymbolicLink')) {
            $target = @($existing.Target)[0]
            if (![IO.Path]::IsPathRooted($target)) { $target = Join-Path $modsDirectory $target }
            if ([IO.Path]::GetFullPath($target).TrimEnd('\') -eq $sourcePath.TrimEnd('\')) { return }
        }
        throw "Mod path already exists and does not link to ${sourcePath}: $linkPath. Update it manually before running tests."
    }
    New-Item -ItemType Junction -Path $linkPath -Target $sourcePath | Out-Null
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
$installedExpansions = @($expansionMetadata | Sort-Object { $_.SelectNodes('/ModMetaData/forceLoadAfter/li').Count } |
    ForEach-Object { $_.ModMetaData.packageId.ToLowerInvariant() })
$selectedExpansions = $installedExpansions
if ($PSBoundParameters.ContainsKey('Expansions')) {
    # Accept both PowerShell arrays and a comma-separated argument passed through -File.
    $requestedNames = @($Expansions | ForEach-Object { $_.Split(',') } | ForEach-Object { $_.Trim().ToLowerInvariant() })
    if ($requestedNames -contains '') { throw '-Expansions contains an empty name; use None to run without expansions.' }
    if ($requestedNames -contains 'none') {
        if ($requestedNames.Count -ne 1) { throw '-Expansions None cannot be combined with other names.' }
        $selectedExpansions = @()
    }
    else {
        $requestedPackages = @(foreach ($name in $requestedNames) {
            $package = "ludeon.rimworld.$name"
            if ($installedExpansions -notcontains $package) {
                $installedNames = ($installedExpansions | ForEach-Object { $_.Substring('ludeon.rimworld.'.Length) }) -join ', '
                throw "Unknown or uninstalled expansion '$name'. Installed expansions: $installedNames."
            }
            $package
        })
        # Keep the game's load order and include each requested expansion only once.
        $selectedExpansions = @($installedExpansions | Where-Object { $requestedPackages -contains $_ })
    }
}
if ($Suite -eq 'XylXenos' -and $selectedExpansions -notcontains 'ludeon.rimworld.biotech') {
    throw 'XylXenos tests require Biotech; include it in -Expansions.'
}

$repositoryDirectory = Split-Path -Parent $PSScriptRoot
$testProjectDirectory = Join-Path $repositoryDirectory "$Suite.Tests"
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null

$testProject = Join-Path $testProjectDirectory "$Suite.Tests.csproj"
$testBuildArguments = @($testProject, '--configuration', $Configuration, "-p:RimWorldDirectory=$gameDirectory")
if ($PSBoundParameters.ContainsKey('DisharmonySourceDirectory')) {
    if ($DisharmonySourceDirectory) {
        $DisharmonySourceDirectory = (Resolve-Path -LiteralPath $DisharmonySourceDirectory).ProviderPath
    }
    $testBuildArguments += "-p:DisharmonySourceDirectory=$DisharmonySourceDirectory"
}
if (!$SkipBuild) { Invoke-Build $testBuildArguments }
# Select the runtime from the deployed output, including when reusing a Visual Studio build.
$runtimeMod = if ($Suite -eq 'Xylib') { 'Xylib.Tests' } else { $Suite }
$usesBundledDisharmony = Test-Path -LiteralPath (Join-Path $repositoryDirectory "ModBin/$runtimeMod/Assemblies/Disharmony.RimWorld.dll")

$modsDirectory = Join-Path $gameDirectory 'Mods'
$workshopDirectory = [IO.Path]::GetFullPath((Join-Path $gameDirectory '../../workshop/content/294100'))
$workshopPackages = @{}
$workshopIds = @('2009463077')
if (!$usesBundledDisharmony) { $workshopIds += '3818084845' }
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

$testMod = Join-Path $repositoryDirectory "ModBin/$Suite.Tests"
$testPackage = ('xylthixlm.' + $Suite + '.tests').ToLowerInvariant()
Ensure-ModLink 'DevTools' (Join-Path $repositoryDirectory 'ModBin/DevTools')
Ensure-ModLink "$Suite.Tests" $testMod
if ($Suite -ne 'Xylib') {
    $gameplayMod = if ($Suite -eq 'Yobai') { 'Yobai' } else { 'XylXenos' }
    $gameplayPackage = if ($Suite -eq 'Yobai') { 'xylthixlm.yobai' } else { 'xylthixlm.races.core' }
    Ensure-ModLink $gameplayMod (Join-Path $repositoryDirectory "ModBin/$gameplayMod")
}

$saveDataDirectory = Join-Path $OutputDirectory 'UserData'
$configDirectory = Join-Path $saveDataDirectory 'Config'
New-Item -ItemType Directory -Path $configDirectory -Force | Out-Null
$activeMods = @($workshopPackages['brrainz.harmony'], 'ludeon.rimworld')
$activeMods += $selectedExpansions
if (!$usesBundledDisharmony) { $activeMods += $workshopPackages['xylthixlm.disharmony'] }
if ($Suite -ne 'Xylib') { $activeMods += $gameplayPackage }
$activeMods += @('smashphil.devtools', $testPackage)
$activeXml = ($activeMods | ForEach-Object { "<li>$($_)</li>" }) -join ''
$expansionsXml = ($installedExpansions | ForEach-Object { "<li>$($_)</li>" }) -join ''
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
# Keep the built mod's test settings and only redirect this run's report.
[xml] $testConfig = Get-Content -LiteralPath (Join-Path $testConfigDirectory 'UnitTestConfig.xml') -Raw -Encoding utf8
$log = $testConfig.TestConfig.SelectSingleNode('log')
if ($null -eq $log) { $log = $testConfig.TestConfig.AppendChild($testConfig.CreateElement('log')) }
$folder = $log.SelectSingleNode('folder')
if ($null -eq $folder) { $folder = $log.AppendChild($testConfig.CreateElement('folder')) }
$folder.InnerText = $OutputDirectory
$testConfig.Save((Join-Path $testConfigDirectory 'UnitTestConfig.xml'))

$process = $null
$stdoutFile = $null
$stderrFile = $null
$stdout = $null
$stderr = $null
try {
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
    }
}
