using namespace System.Management.Automation.Language;
using namespace System.Management.Automation;

Register-ArgumentCompleter -CommandName Get-KubePod -ParameterName Name -ScriptBlock {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$fakeBoundParameter)

    $fakeBoundParameter.Remove('Name')

    Get-KubePod @fakeBoundParameter |
        ForEach-Object { $_.Metadata.Name } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}

Register-ArgumentCompleter -CommandName Get-KubePod -ParameterName Namespace -ScriptBlock {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$fakeBoundParameter)

    $fakeBoundParameter.Remove('Namespace')
    $fakeBoundParameter.Remove('Name')

    Get-KubeResource @fakeBoundParameter -Kind 'Namespace' -ApiVersion 'v1' |
        ForEach-Object { $_.Metadata.Name } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}
