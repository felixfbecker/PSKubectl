using namespace System.Management.Automation.Language;
using namespace System.Management.Automation;

Register-ArgumentCompleter -CommandName Get-KubeDeployment -ParameterName Name -ScriptBlock {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$params)

    $deployParams = @{ }
    if ($params.ContainsKey('ApiEndpoint')) {
        $deployParams.ApiEndpoint = $params.ApiEndpoint
    }
    if ($params.ContainsKey('AllowInsecure')) {
        $deployParams.AllowInsecure = $params.AllowInsecure
    }
    if ($params.ContainsKey('Namespace')) {
        $deployParams.Namespace = $params.Namespace
    }

    Get-KubeDeployment @deployParams |
        ForEach-Object { $_.Metadata.Name } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}
