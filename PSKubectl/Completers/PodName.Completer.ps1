using namespace System.Management.Automation.Language;
using namespace System.Management.Automation;

$podNameCompleter = {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$params)

    $podParams = @{ }
    if ($params.ContainsKey('ApiEndpoint')) {
        $podParams.ApiEndpoint = $params.ApiEndpoint
    }
    if ($params.ContainsKey('AllowInsecure')) {
        $podParams.AllowInsecure = $params.AllowInsecure
    }
    if ($params.ContainsKey('Namespace')) {
        $podParams.Namespace = $params.Namespace
    }

    Get-KubePod @podParams |
        ForEach-Object { $_.Metadata.Name } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}

Register-ArgumentCompleter -CommandName Get-KubePod -ParameterName Name -ScriptBlock $podNameCompleter
Register-ArgumentCompleter -CommandName Remove-KubePod -ParameterName Name -ScriptBlock $podNameCompleter
Register-ArgumentCompleter -CommandName Get-KubeLog -ParameterName Name -ScriptBlock $podNameCompleter
