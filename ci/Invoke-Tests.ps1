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

    # Run tests in isolated PowerShell instance to not lock coverage and DLL files longer than test run
    pwsh -Command {
        # Import instrumented assemblies
        Import-Module ../PSKubectl/PSKubectl.psd1
        Invoke-Pester ../Tests
    }

    # Output coverage reports
    Invoke-Executable { dotnet minicover report --workdir ../ --threshold 0 }
    Invoke-Executable { dotnet minicover opencoverreport --workdir ../ }
} finally {
    Pop-Location
}
