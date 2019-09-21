Import-Module "$PSScriptRoot/Streams.psm1"
Import-Module "$PSScriptRoot/Invoke-Executable.psm1"

function Initialize-TestNamespace {
    $context = Invoke-Executable { kubectl config current-context }
    if ($context -ne 'docker-desktop' -and $context -ne 'minikube') {
        throw "Kube context is '$context', expected 'docker-for-desktop' or 'minikube'. Aborting for safety reasons."
    }
    Write-Information 'Setting up Kubernetes cluster'
    Invoke-Executable { kubectl apply -f $PSScriptRoot/test.Namespace.yml --experimental-server-side --experimental-force-conflicts } | Out-Stream -SuccessTarget 6
}

function Initialize-TestDeployment {
    Invoke-Executable { kubectl apply -f $PSScriptRoot/test.Deployment.yml --force --experimental-server-side --experimental-force-conflicts --wait } | Out-Stream -SuccessTarget 6
    Invoke-Executable { kubectl rollout status --namespace pskubectltest deploy/hello-world } | Out-Stream -SuccessTarget 6
}
