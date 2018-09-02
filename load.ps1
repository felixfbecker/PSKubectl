param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $NoRestore,
    [switch] $NoBuild
)

Push-Location $PSScriptRoot
try {
    if (-not $NoBuild) {
        ./build.ps1 -Configuration $Configuration -NoRestore:$NoRestore
    }
    pwsh-preview -NoExit -Command "Import-Module -Name '$PSScriptRoot/PSKubectl/PSKubectl.psd1'"
} finally {
    Pop-Location
}
