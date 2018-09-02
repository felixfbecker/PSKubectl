param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $NoRestore,
    [switch] $NoBuild
)

if (-not $NoBuild) {
    ./build.ps1 -Configuration $Configuration -NoRestore:$NoRestore
}
Push-Location "$PSScriptRoot/src"
try {
    pwsh-preview -NoExit -Command "Import-Module -Name '$PSScriptRoot/PSKubectl/PSKubectl.psd1'"
} finally {
    Pop-Location
}
