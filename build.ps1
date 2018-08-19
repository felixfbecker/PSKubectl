param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $NoRestore
)

try {
    Push-Location $PSScriptRoot
    $options = @()
    if ($NoRestore) {
        $options += '--no-restore'
    }
    dotnet publish -o out -c $Configuration @options
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
} finally {
    Pop-Location
}
