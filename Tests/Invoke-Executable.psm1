
function Invoke-Executable {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory)]
        $Command
    )
    &$Command
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $($LASTEXITCODE): $Command"
    }
}
