<#
.SYNOPSIS
    Deletes the test Kubernetes namespace created by tests
#>

$namespace = 'pskubectltest'

Invoke-Executable { kubectl delete namespace $namespace } | Out-Stream -SuccessTarget 6
while (
    (Invoke-Executable { kubectl get pods -n $namespace -o json } | ConvertFrom-Json).Items -or
    (Invoke-Executable { kubectl get deployments -n $namespace -o json } | ConvertFrom-Json).Items -or
    (Invoke-Command { kubectl get namespace $namespace -o json; $LASTEXITCODE -eq 0 } *>&1 | Out-Stream -ErrorTarget $null )
) {
    Write-Information 'Waiting for Kubernetes cluster to be pruned'
    Start-Sleep 1
}
