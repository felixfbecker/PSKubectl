$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'

Install-Module -Scope CurrentUser -Force Pester

$kubectlProxy = Start-Job { kubectl proxy }
try {
    $PSBoundParameters['*-Kube*:ApiEndPoint'] = 'http://127.0.0.1:8001'

    Import-Module "$PSScriptRoot/../PSKubectl/PSKubectl.psd1"
    Invoke-Pester "$PSScriptRoot/../Tests"
} finally {
    Stop-Job -Job $kubectlProxy
}
