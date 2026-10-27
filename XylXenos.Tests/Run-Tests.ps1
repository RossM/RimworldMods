#requires -Version 5.1
[CmdletBinding()]
param(
    [string] $RimWorldPath = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe',
    [string] $OutputDirectory,
    [string] $Filter,
    [ValidateSet('Debug', 'Release')] [string] $Configuration = 'Debug',
    [switch] $SkipBuild,
    [string] $DisharmonySourceDirectory,
    [ValidateNotNullOrEmpty()] [string[]] $Expansions,
    [ValidateRange(1, 120)] [int] $TimeoutMinutes = 10
)

& "$PSScriptRoot/../InGameTests/Run-Tests.ps1" -Suite 'XylXenos' @PSBoundParameters
