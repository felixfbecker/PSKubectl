Import-Module -Force "$PSScriptRoot/Streams.psm1"
Import-Module -Force "$PSScriptRoot/Invoke-Executable.psm1"
Import-Module -Force "$PSScriptRoot/Initialize-TestNamespace.psm1"

Describe Get-KubePod {

    BeforeAll { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should return the pods that exist in a namespace' {
        $pods = Get-KubePod -Namespace pskubectltest
        $pods.Count | Should -Not -BeNullOrEmpty
        $pods | ForEach-Object {
            $_ | Should -BeOfType KubeClient.Models.PodV1
            $_.Name | Should -BeLike 'hello-world-*'
            $_.Namespace | Should -Be 'pskubectltest'
            $_.Status.Phase | Should -Be 'Running'
        }
    }
}
Describe Remove-KubePod {

    BeforeEach { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should delete pods given by wildcard name' {
        $before = Invoke-Executable { kubectl get pods -n pskubectltest -o name }
        $before | Should -Not -BeNullOrEmpty
        Remove-KubePod -Namespace pskubectltest -Name hello-world-* -ErrorAction Stop
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items |
            Where-Object { $_.Metadata.Name -in $before -and $_.Metadata.DeletionTimestamp -eq $null } |
            Should -BeNullOrEmpty
    }

    It 'Should delete pods given by piped name' {
        $before = (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items.Metadata.Name
        $before | Should -Not -BeNullOrEmpty
        $before | Remove-KubePod -Namespace pskubectltest -ErrorAction Stop
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items |
            Where-Object { $_.Metadata.Name -in $before -and $_.Metadata.DeletionTimestamp -eq $null } |
            Should -BeNullOrEmpty
    }

    It 'Should delete pods piped from Get-KubePod' {
        $before = Invoke-Executable { kubectl get pods -n pskubectltest -o name }
        $before | Should -Not -BeNullOrEmpty
        Get-KubePod -Namespace pskubectltest | Remove-KubePod
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items |
            Where-Object { $_.Metadata.Name -in $before -and $_.Metadata.DeletionTimestamp -eq $null } |
            Should -BeNullOrEmpty
    }

    It 'Should delete pods piped from parsed kubectl JSON' {
        $before = Invoke-Executable { kubectl get pods -n pskubectltest -o name }
        $before | Should -Not -BeNullOrEmpty
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items | Remove-KubePod
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items |
            Where-Object { $_.Metadata.Name -in $before -and $_.Metadata.DeletionTimestamp -eq $null } |
            Should -BeNullOrEmpty
    }

    It 'Should delete a pod by name' {
        $name = (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items[0].Metadata.Name
        $name | Should -Not -BeNullOrEmpty
        Remove-KubePod -Name $name -Namespace pskubectltest
        $pod = (Invoke-Executable { kubectl get pods -n pskubectltest -o json $name } | ConvertFrom-Json)
        if ($pod) {
            $pod.Metadata.DeletionTimestamp | Should -Not -BeNullOrEmpty
        }
    }

    It 'Should delete a pod by name with prefix' {
        $name = (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items[0].Metadata.Name
        $name | Should -Not -BeNullOrEmpty
        Remove-KubePod -Name "pod/$name" -Namespace pskubectltest
        $pod = (Invoke-Executable { kubectl get pods -n pskubectltest -o json $name } | ConvertFrom-Json)
        if ($pod) {
            $pod.Metadata.DeletionTimestamp | Should -Not -BeNullOrEmpty
        }
    }
}

Describe Get-KubeResource {

    BeforeAll { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should return pods in a namespace' {
        $pods = Get-KubeResource Pod -Namespace pskubectltest
        $pods.Count | Should -Not -BeNullOrEmpty
        $pods | ForEach-Object {
            $_ | Should -BeOfType KubeClient.Models.PodV1
            $_.Name | Should -BeLike 'hello-world-*'
            $_.Namespace | Should -Be 'pskubectltest'
            $_.Status.Phase | Should -Be 'Running'
        }
    }
}


Describe Get-KubeDeployment {

    BeforeAll { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should return the deployments that exist in a namespace' {
        $deploy = Get-KubeDeployment -Namespace pskubectltest
        $deploy | Should -HaveCount 1
        $deploy | Should -BeOfType KubeClient.Models.DeploymentV1
        $deploy.Name | Should -Be 'hello-world'
        $deploy.Namespace | Should -Be 'pskubectltest'
        $deploy.Desired | Should -Be 2
        $deploy.Current | Should -Be 2
    }
}

Describe Get-KubeNamespace {

    BeforeAll { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should return the deployments that exist in a namespace' {
        $namespaces = Get-KubeNamespace
        $namespaces | Where-Object { $_.Name -eq 'pskubectltest' } | Should -Not -BeNullOrEmpty
        $namespaces | Should -BeOfType KubeClient.Models.NamespaceV1
    }
}

Describe Update-KubeResource {
    BeforeAll { Initialize-TestNamespace }
    BeforeEach {
        # Delete everything for each test to make sure field managers are not messed up.
        Invoke-Executable { kubectl delete -n pskubectltest --all --wait deploy } | Out-Stream -SuccessTarget 6
    }

    Describe 'Updating' {

        BeforeEach {
            Initialize-TestDeployment
        }

        It 'Should update the resource from PSCustomObject pipeline input' {
            $before = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $before.Metadata.Annotations.hello | Should -Be 'world'
            $modified = [PSCustomObject]@{
                Kind = 'Deployment'
                ApiVersion = 'apps/v1'
                Metadata = [PSCustomObject]@{
                    Name = 'hello-world'
                    Namespace = 'pskubectltest'
                    Annotations = @{
                        'hello' = 'changed'
                    }
                }
                Spec = [PSCustomObject]@{
                    Selector = [PSCustomObject]@{
                        MatchLabels = [PSCustomObject]@{
                            App = 'hello-world'
                        }
                    }
                    Replicas = 2
                    Template = [PSCustomObject]@{
                        Metadata = [PSCustomObject]@{
                            Labels = [PSCustomObject]@{
                                App = 'hello-world'
                            }
                        }
                        Spec = [PSCustomObject]@{
                            Containers = @(
                                [PSCustomObject]@{
                                    Name = 'hello-world'
                                    Image = 'strm/helloworld-http@sha256:bd44b0ca80c26b5eba984bf498a9c3bab0eb1c59d30d8df3cb2c073937ee4e45'
                                    ImagePullPolicy = 'IfNotPresent'
                                    Ports = @(
                                        [PSCustomObject]@{
                                            ContainerPort = 80
                                            Protocol = 'TCP'
                                        }
                                    )
                                }
                            )
                        }
                    }
                }
            }
            $result = $modified | Update-KubeResource
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1
            $result.Metadata.Annotations['hello'] | Should -Be 'changed'
            $after = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $after.Metadata.Annotations.hello | Should -Be 'changed'
        }

        It 'Should update the resource from a path to a YAML file' {
            $before = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $before.Metadata.Annotations.hello | Should -Be 'world'
            $result = Update-KubeResource -Path $PSScriptRoot/modified.Deployment.yml
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1
            $result.Metadata.Annotations['hello'] | Should -Be 'changed'
            $after = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $after.Metadata.Annotations.hello | Should -Be 'changed'
        }
    }

    Describe 'Updating with conflicts' {

        # See https://github.com/kubernetes/kubernetes/issues/80916#issuecomment-525049458 for an explanation of the behaviour

        It 'Should fail with a Conflict error if it was with kubectl create or client-side apply before' {
            Invoke-Executable { kubectl create -f $PSScriptRoot/test.Deployment.yml }
            Invoke-Executable { kubectl rollout status --namespace pskubectltest deploy/hello-world } | Out-Stream -SuccessTarget 6
            { Update-KubeResource -Path $PSScriptRoot/modified.Deployment.yml } | Should -Throw "Conflict"
        }

        It 'Should update the resource if it was updated before with kubectl create or client-side apply and -Force was given' {
            Invoke-Executable { kubectl create -f $PSScriptRoot/test.Deployment.yml }
            Invoke-Executable { kubectl rollout status --namespace pskubectltest deploy/hello-world } | Out-Stream -SuccessTarget 6

            $before = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $before.Metadata.Annotations.hello | Should -Be 'world'

            $result = Update-KubeResource -Path $PSScriptRoot/modified.Deployment.yml -Force
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1
            $result.Metadata.Annotations.hello | Should -Be 'changed'

            $after = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $after.Metadata.Annotations.hello | Should -Be 'changed'
        }
    }

    Describe 'Create' {
        It 'Should create a resource from a path to a YAML file' {
            # Sanity check: Make sure doesn't exist yet
            $before = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $before | Should -BeNullOrEmpty

            $result = Update-KubeResource -Path $PSScriptRoot/test.Deployment.yml
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1

            $after = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $after.metadata.name | Should -Be 'hello-world'
            $after.metadata.annotations.hello | Should -Be 'world'
            $after.spec.selector.matchLabels.app | Should -Be 'hello-world'
            $after.spec.replicas | Should -Be 2
            $after.spec.template.metadata.labels.app | Should -Be 'hello-world'
            $after.spec.template.spec.containers[0].name | Should -Be 'hello-world'
            $after.spec.template.spec.containers[0].image | Should -Be 'strm/helloworld-http@sha256:bd44b0ca80c26b5eba984bf498a9c3bab0eb1c59d30d8df3cb2c073937ee4e45'
            $after.spec.template.spec.containers[0].imagePullPolicy | Should -Be 'IfNotPresent'
            $after.spec.template.spec.containers[0].ports[0].containerPort | Should -Be 80
            $after.spec.template.spec.containers[0].ports[0].protocol | Should -Be 'TCP'
        }
    }
}

Describe Get-KubeConfig {

    It 'Should return kube configuration' {
        $config = Get-KubeConfig
        $config.CurrentContextName | Should -Not -BeNullOrEmpty
        $config.CurrentContext | Should -Not -BeNullOrEmpty
        $config.Clusters | Should -Not -BeNullOrEmpty
        $config.Contexts | Should -Not -BeNullOrEmpty
    }
}
