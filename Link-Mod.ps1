#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ModsDirectory,
    [Parameter(Mandatory)] [string] $ModDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ModsDirectory = (Resolve-Path -LiteralPath $ModsDirectory).ProviderPath
$source = Get-Item -LiteralPath $ModDirectory -Force
if (!$source.PSIsContainer) { throw "Mod output must be a directory: $ModDirectory" }

$sourcePath = $source.FullName
$linkPath = Join-Path $ModsDirectory $source.Name
$existing = Get-Item -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue
if ($null -eq $existing) {
    try {
        New-Item -ItemType Junction -Path $linkPath -Target $sourcePath | Out-Null
        return
    } catch {
        # Another build may have created the link while this one was checking for it.
        $existing = Get-Item -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue
        if ($null -eq $existing) { throw }
    }
}

if ($existing.LinkType -in @('Junction', 'SymbolicLink')) {
    $target = @($existing.Target)[0]
    if (![IO.Path]::IsPathRooted($target)) { $target = Join-Path $ModsDirectory $target }
    if ([IO.Path]::GetFullPath($target).TrimEnd('\') -eq $sourcePath.TrimEnd('\')) { return }
}

throw "Mod path already exists and does not link to ${sourcePath}: $linkPath. Update it manually."
