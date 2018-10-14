$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'

Install-Module -Scope CurrentUser -Force Pester

Import-Module "$PSScriptRoot/../Tests/Invoke-Executable.psm1"

# Need to switch to src/ so dotnet CLI finds the minicover tool
Push-Location "$PSScriptRoot/../src"
try {
    # Instrument assemblies for code coverage prior to importing
    Invoke-Executable { dotnet minicover instrument --workdir ../ --assemblies ./PSKubectl/Assemblies/PSKubectl.dll --sources './src/**/*.cs' }

    # Start kubectl proxy in the background (until we support auth properly)
    $kubectlProxy = Start-Job { kubectl proxy }
    try {
        # Run tests in isolated PowerShell instance to not lock coverage and DLL files longer than test run
        pwsh -Command {
            # Point all commands to the kubectl proxy
            $PSBoundParameters['*-Kube*:ApiEndPoint'] = 'http://127.0.0.1:8001'
            # Import instrumented assemblies
            Import-Module ../PSKubectl/PSKubectl.psd1
            Invoke-Pester ../Tests
        }
    } finally {
        # Stop proxy
        Stop-Job -Job $kubectlProxy
    }

    # Output coverage reports
    Invoke-Executable { dotnet minicover report --workdir ../ --threshold 0 }
    Invoke-Executable { dotnet minicover opencoverreport --workdir ../ }
} finally {
    Pop-Location
}
