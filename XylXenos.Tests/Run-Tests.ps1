#requires -Version 5.1
[CmdletBinding()]
param(
    [string] $RimWorldPath = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe',
    [string] $OutputDirectory,
    [string] $Filter,
    [string] $DisharmonySourceDirectory,
    [ValidateNotNullOrEmpty()] [string[]] $Expansions,
    [ValidateRange(1, 120)] [int] $TimeoutMinutes = 10
)

& "$PSScriptRoot/../InGameTests/Run-Tests.ps1" -Suite 'XylXenos' @PSBoundParameters
