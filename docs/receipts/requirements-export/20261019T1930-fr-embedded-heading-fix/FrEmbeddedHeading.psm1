#Requires -Version 7.4
<#
.SYNOPSIS
    Pure transform for the 10 FR records whose description embeds an old rendered copy of its own section heading.
    Remove-FrEmbeddedHeading removes only the first line when it is "## <ID>: <text>" or "## <ID> <text>" for that
    record's own id, plus the blank lines directly after it. Everything else in the description is kept byte for byte
    (Iteration, the old title variant, Source References and Traceability exist nowhere else in the record).
    Anything else (no heading, another id's heading, a heading not on the first line) throws, so a wrong record can
    never be rewritten.
#>
Set-StrictMode -Version Latest

function Remove-FrEmbeddedHeading {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'Pure string transform; it changes no state.')]
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Description
    )
    $m = [regex]::Match($Description, '\A##[ \t]+' + [regex]::Escape($Id) + '(?::|[ \t])[^\r\n]*(\r?\n)+')
    if (-not $m.Success) { throw "description of $Id does not start with its own embedded '## $Id' heading" }
    $rest = $Description.Substring($m.Length)
    if ($rest.Length -eq 0) { throw "description of $Id would be empty after removing the heading" }
    return $rest
}

Export-ModuleMember -Function Remove-FrEmbeddedHeading
