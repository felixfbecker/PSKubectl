param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $NoRestore
)

Push-Location $PSScriptRoot
try {
    ./build.ps1 -Configuration $Configuration -NoRestore:$NoRestore
    pwsh-preview -NoExit -Command "Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath PSKubectl.psd1)"
} finally {
    Pop-Location
}
