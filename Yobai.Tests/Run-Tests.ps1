#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RimWorldPath,
    [string] $OutputDirectory,
    [string] $Filter,
    [switch] $CoreOnly,
    [ValidateRange(1, 120)] [int] $TimeoutMinutes = 10
)

& "$PSScriptRoot/../InGameTests/Run-Tests.ps1" -Suite 'Yobai' @PSBoundParameters
