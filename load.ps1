param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $NoRestore,
    [switch] $NoBuild
)

&"$PSScriptRoot/build.ps1" -Configuration $Configuration -NoRestore:$NoRestore
pwsh-preview -NoExit -Command "Import-Module -Name '$PSScriptRoot/PSKubectl/PSKubectl.psd1'"
