param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

dotnet publish -o out -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Build failed"
}
