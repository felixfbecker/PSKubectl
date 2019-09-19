Import-Module "$PSScriptRoot/Streams.psm1"
Import-Module "$PSScriptRoot/Invoke-Executable.psm1"

function Initialize-TestNamespace {
    $context = Invoke-Executable { kubectl config current-context }
    if ($context -ne 'docker-for-desktop' -and $context -ne 'minikube') {
        throw "Kube context is '$context', expected 'docker-for-desktop' or 'minikube'. Aborting for safety reasons."
    }
    $namespace = 'pskubectltest'
    Write-Information 'Setting up Kubernetes cluster'
    Invoke-Executable { kubectl apply -f $PSScriptRoot/test.Namespace.yml --force } | Out-Stream -SuccessTarget 6
}

function Initialize-TestDeployment {
    Invoke-Executable { kubectl apply -f $PSScriptRoot/test.Deployment.yml --force } | Out-Stream -SuccessTarget 6
    Invoke-Executable { kubectl rollout status deploy/hello-world -n $namespace } | Out-Stream -SuccessTarget 6
}
