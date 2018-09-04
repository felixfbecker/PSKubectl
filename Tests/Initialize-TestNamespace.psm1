Import-Module "$PSScriptRoot/Streams.psm1"
Import-Module "$PSScriptRoot/Invoke-Executable.psm1"

function Initialize-TestNamespace {
    $namespace = 'pskubectltest'
    Write-Information 'Setting up Kubernetes cluster'
    Invoke-Executable { kubectl apply -f $PSScriptRoot/test.Namespace.yml } | Out-Stream -SuccessTarget 6
    Invoke-Executable { kubectl apply -f $PSScriptRoot/test.Deployment.yml } | Out-Stream -SuccessTarget 6
    Invoke-Executable { kubectl rollout status deploy/hello-world -n $namespace } | Out-Stream -SuccessTarget 6
}
