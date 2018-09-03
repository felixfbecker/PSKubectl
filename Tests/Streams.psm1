using namespace System.Management.Automation;

function Out-TargetStream {
    <#
    .SYNOPSIS
        Sends the pipeline input to the given target stream. Coerces the input if needed.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [ValidateRange(0, 6)]
        [int] $Target,

        # A union of all parameters of the Write cmdlets
        # Error
        [Parameter(ValueFromPipeline)]
        [ErrorRecord] $ErrorRecord,

        # Debug, Verbose, Warning
        [Parameter(ValueFromPipelineByPropertyName, ValueFromPipeline)]
        [Alias('MessageData')] # Information
        $Message,

        # Information
        [Parameter(ValueFromPipelineByPropertyName)]
        [string[]] $Tags
    )
    switch ($Target) {
        1 { $Message }
        2 {
            if ($null -ne $ErrorRecord) {
                Write-Error -ErrorRecord $ErrorRecord
            } else {
                Write-Error -Message $Message
            }
        }
        3 { Write-Warning -Message $Message }
        4 { Write-Verbose -Message $Message }
        5 { Write-Debug -Message $Message }
        6 { Write-Information -MessageData $Message -Tags $Tags }
    }
}

function Out-Stream {
    <#
    .SYNOPSIS
        Redirects streams to other streams
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        $InputObject,

        [ValidateRange(0, 6)] [int] $SuccessTarget = 1,
        [ValidateRange(0, 6)] [int] $ErrorTarget = 2,
        [ValidateRange(0, 6)] [int] $WarningTarget = 3,
        [ValidateRange(0, 6)] [int] $VerboseTarget = 4,
        [ValidateRange(0, 6)] [int] $DebugTarget = 5,
        [ValidateRange(0, 6)] [int] $InformationTarget = 6
    )
    process {
        if ($_ -is [ErrorRecord]) {
            $_ | Out-TargetStream -Target $ErrorTarget
        } elseif ($_ -is [InformationRecord]) {
            $_ | Out-TargetStream -Target $InformationTarget
        } elseif ($_ -is [WarningRecord]) {
            $_ | Out-TargetStream -Target $WarningTarget
        } elseif ($_ -is [VerboseRecord]) {
            $_ | Out-TargetStream -Target $VerboseTarget
        } elseif ($_ -is [DebugRecord]) {
            $_ | Out-TargetStream -Target $DebugTarget
        } else {
            $_ | Out-TargetStream -Target $SuccessTarget
        }
    }
}

Export-ModuleMember -Function Out-Stream
