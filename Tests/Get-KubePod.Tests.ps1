Import-Module "$PSScriptRoot/Streams.psm1"
Import-Module "$PSScriptRoot/Invoke-Executable.psm1"

Describe 'Get-KubePod' {

    BeforeAll {
        $namespace = 'pskubectltest'
        Write-Information 'Setting up Kubernetes cluster'
        Invoke-Executable { kubectl apply -f "$PSScriptRoot/test.yml" } | Out-Stream -SuccessTarget 6
        Invoke-Executable { kubectl rollout status deploy/hello-world -n $namespace } | Out-Stream -SuccessTarget 6
    }

    It 'Should return the pods that exist in a namespace' {
        $pods = Get-KubePod -Namespace $namespace
        $pods.Count | Should -Be 2
        $pods | Should -BeOfType KubeClient.Models.PodV1
    }
}
