[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Pins','Read','Git','Progress','Request','Search')][string]$Operation='Progress',[string[]]$Path,[int]$Start=1,[int]$End=0,[int]$Percent=3,[Nullable[int]]$Eta=$null,[string]$Label='initial',[string]$Pattern,[switch]$FilesOnly)
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
if (-not $PSCmdlet.ShouldProcess($Operation,'Collect bounded read-only evidence and receipt-local records')) { return }
function Assert-SafeReviewText {
    param([string]$Text)
    if ($Text -match '(?im)(?:X-Api-Key\s*[:=]\s*|apiKey\s*[:=]\s*)[\x22\x27]?[A-Za-z0-9+/=_-]{20,}|Bearer\s+[A-Za-z0-9+/=_-]{20,}') { throw 'Credential-shaped output suppressed.' }
}
switch ($Operation) {
    'Search' {
        foreach ($item in $Path) { if ($item -match 'AGENTS-README-FIRST|\.creds|plugin-cache') { throw 'Forbidden search path.' } }
        $rgArgs=@('-n','--glob','!AGENTS-README-FIRST.yaml','--glob','!**/plugin-cache/**')
        if ($FilesOnly) { $rgArgs+= '-l' }
        $text=(& rg @rgArgs -- $Pattern @Path | Out-String)
        Assert-SafeReviewText $text
        Write-Output $text
    }
    'Progress' { [ordered]@{timestampLocal=[DateTime]::Now.ToString('s');completionPercent=$Percent;etaSeconds=$Eta} | ConvertTo-Json -Compress | Write-Output }
    'Request' {
        $request=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
        $text=[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');reviewer=$request.reviewer;parentSessionId=$request.parentSessionId;parentRequestId=$request.parentRequestId;arguments=$request.arguments;environment=$request.environment;requestPath=(Join-Path $PSScriptRoot 'request.jsonl');responsePath=$request.responsePath} | ConvertTo-Json -Depth 20
        Assert-SafeReviewText $text
        $text | Write-Output
    }
    'Pins' {
        $pins=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $checks=@(foreach ($pin in $pins) {
            if ($pin.path -match 'AGENTS-README-FIRST|\.creds') { throw 'Forbidden pin path.' }
            $hash=(Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            [ordered]@{path=$pin.relativePath;expected=$pin.sha256;actual=$hash;bytes=(Get-Item -LiteralPath $pin.path -Force).Length;match=($hash -ceq $pin.sha256)}
        })
        $record=[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');count=$checks.Count;driftCount=@($checks | Where-Object { -not $_.match }).Count;pins=$checks}
        $record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'-pins.json')) -Encoding utf8
        $record | ConvertTo-Json -Depth 10 | Write-Output
    }
    'Read' {
        foreach ($item in $Path) {
            if ($item -match 'AGENTS-README-FIRST|\.creds|session-state\.yaml') { throw 'Forbidden content path.' }
            $lines=[IO.File]::ReadAllLines((Join-Path (Get-Location).Path $item))
            $last=if ($End) { [Math]::Min($End,$lines.Length) } else { $lines.Length }
            $text=@("FILE: $item ($($lines.Length) lines)";for ($index=$Start-1;$index -lt $last;$index++) { '{0}: {1}' -f ($index+1),$lines[$index] }) -join "`n"
            Assert-SafeReviewText $text
            Write-Output $text
        }
    }
    'Git' {
        $records=[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');head=(& git rev-parse HEAD);branch=(& git branch --show-current);status=@(& git status --porcelain=v1 -uall);nativeStatus=@(& git -C native/vice status --porcelain=v1 -uall);nativeStat=@(& git -C native/vice diff --stat);submodules=@(& git submodule status)}
        $records | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'-git.json')) -Encoding utf8
        $records | ConvertTo-Json -Depth 10 | Write-Output
    }
}
