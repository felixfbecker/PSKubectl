param(
    [Parameter(Mandatory)][string]$KubectlVersion,
    [Parameter(Mandatory)][string]$MinikubeVersion,
    [Parameter(Mandatory)][string]$KubernetesVersion
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$InformationPreference = 'Continue'

Import-Module "$PSScriptRoot/../Tests/Invoke-Executable.psm1"

$env:CHANGE_MINIKUBE_NONE_USER = 'true'

# Download kubectl, which is a requirement for using minikube.
Invoke-WebRequest -OutFile kubectl -Uri "https://storage.googleapis.com/kubernetes-release/release/$KubectlVersion/bin/linux/amd64/kubectl"
Invoke-Executable { chmod +x kubectl }
Invoke-Executable { sudo mv kubectl /usr/local/bin/ }

# Download minikube.
Invoke-WebRequest -OutFile minikube -Uri "https://storage.googleapis.com/minikube/releases/$MinikubeVersion/minikube-linux-amd64"
Invoke-Executable { chmod +x minikube }
Invoke-Executable { sudo mv minikube /usr/local/bin/ }
Invoke-Executable { sudo minikube start --vm-driver=none --kubernetes-version=$KubernetesVersion }

# Fix the kubectl context, as it's often stale.
Invoke-Executable { minikube update-context }
# Output debug information
Write-Information "Current context: $(Invoke-Executable { kubectl config current-context })"

# Little helper to poll a command until the resource status reports a {Type: Ready, Status: True} condition
function Wait-KubeConditions([scriptblock] $Command, [string]$Label) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($true) {
        try {
            Write-Information "Waiting for $Label to be ready"
            $resources = (Invoke-Executable $Command | ConvertFrom-Json).Items
            $conditions = $resources.Status.Conditions
            $conditions | Format-Table -Property Type, Status, Reason, Message
            if (($conditions | Where-Object { $_.Type -eq 'Ready' -and $_.Status -eq 'True' })) {
                break
            }
        } catch {
            Write-Warning $_
        }
        Start-Sleep 1
        if ($timer.Elapsed.TotalSeconds -gt 60) {
            throw "Timed out after 60s waiting for $Label to become ready"
        }
    }
    $timer.Stop()
    Write-Information "$Label ready"
}

# Wait for Kubernetes to be up and ready.
Wait-KubeConditions -Command { kubectl get nodes -o json } -Label 'Node'

Invoke-Executable { kubectl cluster-info }

# Verify kube-addon-manager.
# kube-addon-manager is responsible for managing other kubernetes components, such as kube-dns, dashboard, storage-provisioner..
Wait-KubeConditions -Command { kubectl -n kube-system get pods -lcomponent=kube-addon-manager -o json } -Label 'kube-addon-manager'

# Wait for kube-dns to be ready.
Wait-KubeConditions -Command { kubectl -n kube-system get pods -lk8s-app=kube-dns -o json } -Label 'kube-dns'
