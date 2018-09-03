using namespace System.Management.Automation.Language;
using namespace System.Management.Automation;

$podNameCompleter = {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$fakeBoundParameter)

    $fakeBoundParameter.Remove('Name')

    Get-KubePod @fakeBoundParameter |
        ForEach-Object { $_.Metadata.Name } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}

Register-ArgumentCompleter -CommandName Get-KubePod -ParameterName Name -ScriptBlock $podNameCompleter
Register-ArgumentCompleter -CommandName Get-KubeLog -ParameterName Name -ScriptBlock $podNameCompleter
