using namespace System.Management.Automation.Language;
using namespace System.Management.Automation;

$namespaceCompleter = {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$params)

    $nsParams = @{ }
    if ($params.ContainsKey('ApiEndpoint')) {
        $nsParams.ApiEndpoint = $params.ApiEndpoint
    }
    if ($params.ContainsKey('AllowInsecure')) {
        $nsParams.AllowInsecure = $params.AllowInsecure
    }

    Get-KubeNamespace @nsParams |
        ForEach-Object { $_.Metadata.Name } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}

Register-ArgumentCompleter -CommandName Get-KubePod -ParameterName Namespace -ScriptBlock $namespaceCompleter
Register-ArgumentCompleter -CommandName Remove-KubePod -ParameterName Namespace -ScriptBlock $namespaceCompleter
Register-ArgumentCompleter -CommandName Get-KubeResource -ParameterName Namespace -ScriptBlock $namespaceCompleter
Register-ArgumentCompleter -CommandName Get-KubeDeployment -ParameterName Namespace -ScriptBlock $namespaceCompleter
Register-ArgumentCompleter -CommandName Get-KubeLog -ParameterName Namespace -ScriptBlock $namespaceCompleter
