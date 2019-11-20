Import-Module -Force "$PSScriptRoot/Streams.psm1"
Import-Module -Force "$PSScriptRoot/Invoke-Executable.psm1"
Import-Module -Force "$PSScriptRoot/Initialize-TestNamespace.psm1"

Describe Get-KubePod {

    BeforeAll { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should return the pods that exist in a namespace' {
        $pods = Get-KubePod -Namespace pskubectltest
        $pods | Should -Not -BeNullOrEmpty
        $pods | ForEach-Object {
            $_ | Should -BeOfType KubeClient.Models.PodV1
            $_.Name | Should -BeLike 'hello-world-*'
            $_.Namespace | Should -Be 'pskubectltest'
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

Describe Remove-KubeResource {

    BeforeEach { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should delete a pod by name' {
        $name = (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items[0].Metadata.Name
        $name | Should -Not -BeNullOrEmpty
        Remove-KubeResource -Kind pod -Name $name -Namespace pskubectltest
        $pod = (Invoke-Executable { kubectl get pods -n pskubectltest -o json $name } | ConvertFrom-Json)
        if ($pod) {
            $pod.Metadata.DeletionTimestamp | Should -Not -BeNullOrEmpty
        }
    }

    It 'Should delete pods given by wildcard name' {
        $before = Invoke-Executable { kubectl get pods -n pskubectltest -o name }
        $before | Should -Not -BeNullOrEmpty
        Remove-KubeResource -Kind pod -Namespace pskubectltest -Name hello-world-* -ErrorAction Stop
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items |
            Where-Object { $_.Metadata.Name -in $before -and $_.Metadata.DeletionTimestamp -eq $null } |
            Should -BeNullOrEmpty
    }

    It 'Should delete pods piped from Get-KubePod' {
        $before = Invoke-Executable { kubectl get pods -n pskubectltest -o name }
        $before | Should -Not -BeNullOrEmpty
        Get-KubePod -Namespace pskubectltest | Remove-KubeResource
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items |
            Where-Object { $_.Metadata.Name -in $before -and $_.Metadata.DeletionTimestamp -eq $null } |
            Should -BeNullOrEmpty
    }

    It 'Should delete pods piped from parsed kubectl JSON' {
        $before = Invoke-Executable { kubectl get pods -n pskubectltest -o name }
        $before | Should -Not -BeNullOrEmpty
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items | Remove-KubeResource
        (Invoke-Executable { kubectl get pods -n pskubectltest -o json } | ConvertFrom-Json).Items |
            Where-Object { $_.Metadata.Name -in $before -and $_.Metadata.DeletionTimestamp -eq $null } |
            Should -BeNullOrEmpty
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

    It 'Should return deployment by name' {
        $deploy = Get-KubeResource Deployment -Namespace pskubectltest -Name hello-world
        $deploy | Should -HaveCount 1
        $deploy.Metadata.Name | Should -Be 'hello-world'
    }
}


Describe Get-KubeDeployment {

    BeforeAll { Initialize-TestNamespace; Initialize-TestDeployment }

    It 'Should return the deployments that exist in a namespace' {
        $deploy = Get-KubeDeployment -Namespace pskubectltest
        $deploy | Should -Not -BeNullOrEmpty
        $deploy | Should -BeOfType KubeClient.Models.DeploymentV1
        $helloWorld = $deploy | Where-Object { $_.Name -eq 'hello-world' }
        $helloWorld | Should -Not -BeNullOrEmpty
        $helloWorld.Name | Should -Be 'hello-world'
        $helloWorld.Namespace | Should -Be 'pskubectltest'
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

Describe Publish-KubeResource {
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
            $before = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
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
                                    Image = 'kitematic/hello-world-nginx@sha256:ec0ca6dcb034916784c988b4f2432716e2e92b995ac606e080c7a54b52b87066'
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
            $result = $modified | Publish-KubeResource
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1
            $result.Metadata.Annotations['hello'] | Should -Be 'changed'
            $after = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
            $after.Metadata.Annotations.hello | Should -Be 'changed'
        }

        It -Skip 'Should update the resource from modified Get-KubeResource pipeline input' {
            $before = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
            $before.Metadata.Annotations.hello | Should -Be 'world'
            $modified = Get-KubeResource -Kind Deployment -Namespace pskubectltest -Name hello-world
            $modified.Metadata.Annotations['hello'] = 'changed'
            $result = $modified | Publish-KubeResource
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1
            $result.Metadata.Annotations['hello'] | Should -Be 'changed'
            $after = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
            $after.Metadata.Annotations.hello | Should -Be 'changed'
        }

        It 'Should update the resource from a path to a YAML file' {
            $before = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
            $before.Metadata.Annotations.hello | Should -Be 'world'
            $result = Publish-KubeResource -Path $PSScriptRoot/modified.Deployment.yml
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1
            $result.Metadata.Annotations['hello'] | Should -Be 'changed'
            $after = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
            $after.Metadata.Annotations.hello | Should -Be 'changed'
        }
    }

    Describe 'Updating with conflicts' {

        # See https://github.com/kubernetes/kubernetes/issues/80916#issuecomment-525049458 for an explanation of the behaviour

        It 'Should fail with a Conflict error if it was with kubectl create or client-side apply before' {
            Invoke-Executable { kubectl create -f $PSScriptRoot/test.Deployment.yml }
            Invoke-Executable { kubectl rollout status --namespace pskubectltest deploy/hello-world } | Out-Stream -SuccessTarget 6
            { Publish-KubeResource -Path $PSScriptRoot/modified.Deployment.yml } | Should -Throw "Conflict"
        }

        It 'Should update the resource if it was updated before with kubectl create or client-side apply and -Force was given' {
            Invoke-Executable { kubectl create -f $PSScriptRoot/test.Deployment.yml }
            Invoke-Executable { kubectl rollout status --namespace pskubectltest deploy/hello-world } | Out-Stream -SuccessTarget 6

            $before = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
            $before.Metadata.Annotations.hello | Should -Be 'world'

            $result = Publish-KubeResource -Path $PSScriptRoot/modified.Deployment.yml -Force
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1
            $result.Metadata.Annotations.hello | Should -Be 'changed'

            $after = (Invoke-Executable { kubectl get deploy hello-world -n pskubectltest -o json } | ConvertFrom-Json)
            $after.Metadata.Annotations.hello | Should -Be 'changed'
        }
    }

    Describe 'Create' {
        It 'Should create a resource from a path to a YAML file' {
            # Sanity check: Make sure doesn't exist yet
            $before = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
            $before | Should -BeNullOrEmpty

            $result = Publish-KubeResource -Path $PSScriptRoot/test.Deployment.yml
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType KubeClient.Models.DeploymentV1

            $after = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json hello-world } | ConvertFrom-Json)
            $after.metadata.name | Should -Be 'hello-world'
            $after.metadata.annotations.hello | Should -Be 'world'
            $after.spec.selector.matchLabels.app | Should -Be 'hello-world'
            $after.spec.replicas | Should -Be 2
            $after.spec.template.metadata.labels.app | Should -Be 'hello-world'
            $after.spec.template.spec.containers[0].name | Should -Be 'hello-world'
            $after.spec.template.spec.containers[0].imagePullPolicy | Should -Be 'IfNotPresent'
            $after.spec.template.spec.containers[0].ports[0].containerPort | Should -Be 80
            $after.spec.template.spec.containers[0].ports[0].protocol | Should -Be 'TCP'
        }
    }
}

Describe Get-KubeResourceKinds {
    It 'Should return resource kinds' {
        $kinds = Get-KubeResourceKinds | ForEach-Object Kind
        $kinds | Should -Contain 'Deployment'
        $kinds | Should -Contain 'Pod'
    }
}

Describe Get-KubeLog {
    BeforeEach {
        Initialize-TestNamespace
        Initialize-TestDeployment
    }

    It 'Should return the logs of a given pod' {
        $logs = Get-KubeResource Pod -Namespace pskubectltest -Name hello-world-* | Select-Object -First 1 | Get-KubeLog
        $logs.Contains('nginx') | Should -BeTrue
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

Describe ConvertFrom-KubeYaml {
    It 'Should read in YAML' {
        $parsed = Get-Content -Raw $PSScriptRoot/test.Deployment.yml | ConvertFrom-KubeYaml
        $parsed.PSObject.TypeNames | Should -Contain 'KubeClient.Models.DeploymentV1'
        $parsed.Metadata.Name | Should -Be 'hello-world'
        $parsed.Spec.Replicas | Should -Be 2
    }
    It 'Should round-trip' {
        $yaml = Get-Content -Raw $PSScriptRoot/test.Deployment.yml
        $yaml | ConvertFrom-KubeYaml | ConvertTo-KubeYaml | Should -Be $yaml
    }
}

Describe ConvertTo-KubeYaml {
    BeforeAll {
        Initialize-TestNamespace
        Initialize-TestDeployment
    }

    It 'Should encode PSCustomObjects' {
        $deploy = [PSCustomObject]@{
            Kind = 'Deployment'
            ApiVersion = 'apps/v1'
            Metadata = [PSCustomObject]@{
                Name = 'hello-world'
                Namespace = 'pskubectltest'
                Annotations = @{
                    'hello' = 'changed'
                }
            }
        }
        $yaml = @(
            'kind: Deployment',
            'apiVersion: apps/v1',
            'metadata:',
            '  name: hello-world',
            '  namespace: pskubectltest',
            '  annotations:',
            '    hello: changed',
            ''
        ) -join "`n"
        $deploy | ConvertTo-KubeYaml | Should -Be $yaml
    }

    It 'Should encode Get-KubeResource output to YAML' {
        $parsed = Get-KubeResource -Kind Deployment -Namespace pskubectltest -Name hello-world | ConvertTo-KubeYaml | ConvertFrom-KubeYaml
        $parsed.Metadata.Name | Should -Be 'hello-world'
        $parsed.Spec.Replicas | Should -Be 2
        $parsed.Status.Replicas | Should -Be 2
    }
}
