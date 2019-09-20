Import-Module "$PSScriptRoot/Streams.psm1"
Import-Module "$PSScriptRoot/Invoke-Executable.psm1"
Import-Module "$PSScriptRoot/Initialize-TestNamespace.psm1"

Describe Get-KubePod {

    BeforeAll { Initialize-TestNamespace }

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

    BeforeEach { Initialize-TestNamespace }

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

    BeforeAll { Initialize-TestNamespace }

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

    BeforeAll { Initialize-TestNamespace }

    It 'Should return the deployments that exist in a namespace' {
        $deploy = Get-KubeDeployment -Namespace pskubectltest
        $deploy.Count | Should -Be 1
        $deploy | Should -BeOfType KubeClient.Models.DeploymentV1
        $deploy.Name | Should -Be 'hello-world'
        $deploy.Namespace | Should -Be 'pskubectltest'
        $deploy.Desired | Should -Be 2
        $deploy.Current | Should -Be 2
    }
}

Describe Get-KubeNamespace {

    BeforeAll { Initialize-TestNamespace }

    It 'Should return the deployments that exist in a namespace' {
        $namespaces = Get-KubeNamespace
        $namespaces | Where-Object { $_.Name -eq 'pskubectltest' } | Should -Not -BeNullOrEmpty
        $namespaces | Should -BeOfType KubeClient.Models.NamespaceV1
    }
}

Describe Update-KubeResource {

    BeforeAll { Initialize-TestNamespace }

    It 'Should update the resource from PSCustomObject pipeline input' {
        $before = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
        $before.Metadata.Annotations.hello | Should -Be 'world'
        $modified = [pscustomobject]@{
            Kind = 'Deployment'
            ApiVersion = 'apps/v1'
            Metadata = [pscustomobject]@{
                Name = 'hello-world'
                Namespace = 'pskubectltest'
                Annotations = @{
                    'hello' = 'changed'
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
        $before.Metadata.Annotations['hello'] | Should -Be 'world'
        $result = Update-KubeResource -Path $PSScriptRoot/modified.Deployment.yml -LogPayloads
        $result | Should -Not -BeNullOrEmpty
        $result | Should -BeOfType KubeClient.Models.DeploymentV1
        $result.Metadata.Annotations['hello'] | Should -Be 'changed'
        $after = (Invoke-Executable { kubectl get deploy -n pskubectltest -o json } | ConvertFrom-Json).Items
        $after.Metadata.Annotations.hello | Should -Be 'changed'
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
