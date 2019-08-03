param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $NoRestore
)

try {
    Push-Location "$PSScriptRoot/src"
    $options = @()
    if ($NoRestore) {
        $options += '--no-restore'
    }
    dotnet build --output ../PSKubectl/Assemblies --configuration $Configuration @options
    Remove-Item  ../PSKubectl/Assemblies/Newtonsoft.Json.dll # PowerShell Core ships with this DLL already and cannot load 2 DLLs of the same type anyway
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
} finally {
    Pop-Location
}
