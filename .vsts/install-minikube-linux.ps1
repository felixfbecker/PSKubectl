
# Download kubectl, which is a requirement for using minikube.
Invoke-WebRequest -OutFile kubectl -Uri "https://storage.googleapis.com/kubernetes-release/release/v1.11.2/bin/linux/amd64/kubectl"

chmod +x kubectl
if ($LASTEXITCODE -ne 0) { throw "Command failed: chmod +x kubectl" }

sudo mv kubectl /usr/local/bin/
if ($LASTEXITCODE -ne 0) { throw "Command failed: sudo mv kubectl /usr/local/bin/" }

# Download minikube.
Invoke-WebRequest -OutFile minikube -Uri https://storage.googleapis.com/minikube/releases/v0.28.2/minikube-linux-amd64

chmod +x minikube
if ($LASTEXITCODE -ne 0) { throw "Command failed: chmod +x minikube" }

sudo mv minikube /usr/local/bin/
if ($LASTEXITCODE -ne 0) { throw "Command failed: sudo mv minikube /usr/local/bin/" }

sudo minikube start --vm-driver=none "--kubernetes-version=v1.10.7"
if ($LASTEXITCODE -ne 0) { throw "Command failed: sudo minikube start --vm-driver=none --kubernetes-version=v1.10.7" }

# Fix the kubectl context, as it's often stale.
minikube update-context
if ($LASTEXITCODE -ne 0) { throw "Command failed: minikube update-context" }

# Wait for Kubernetes to be up and ready.
while ($true) {
    $nodes = kubectl get nodes -o json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Command failed" }

    # Wait for "Ready" condition
    if ($nodes.Items.Status.Conditions | Where-Object { $_.Type -eq 'Ready' -and $_.Status -eq 'True' }) {
        break
    }

    Write-Information "Waiting for node to be ready..."
    Start-Sleep 1
}

Write-Information "Node is ready"
