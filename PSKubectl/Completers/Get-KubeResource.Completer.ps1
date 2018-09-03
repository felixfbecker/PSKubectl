using namespace System.Management.Automation.Language;
using namespace System.Management.Automation;

Register-ArgumentCompleter -CommandName Get-KubeResource -ParameterName Kind -ScriptBlock {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$fakeBoundParameter)

    Get-KubeResourceKinds |
        ForEach-Object { $_.Kind } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}

Register-ArgumentCompleter -CommandName Get-KubeResource -ParameterName ApiVersion -ScriptBlock {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$fakeBoundParameter)

    Get-KubeResourceKinds |
        ForEach-Object { $_.ApiVersion } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}

Register-ArgumentCompleter -CommandName Get-KubeResource -ParameterName Name -ScriptBlock {
    param([string]$commandName, [string]$parameterName, [string]$wordToComplete, [CommandAst]$commandAst, [Hashtable]$fakeBoundParameter)

    if (-not $fakeBoundParameter.Kind) {
        return
    }

    $fakeBoundParameter.Remove('Name')

    Get-KubeResource @fakeBoundParameter |
        ForEach-Object { $_.Metadata.Name } |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterValue, $_) }
}
