
Push-Location $PSScriptRoot
try {
    ./build.ps1
    pwsh-preview -NoExit -Command "Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath PSKubectl.psd1)"
} finally {
    Pop-Location
}
