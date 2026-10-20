[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Audit', 'QueryParameters', 'RedactPrefix', 'Persistence')][string]$Action)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, "Finalize review receipt: $Action")) { return }
$utf8 = [Text.UTF8Encoding]::new($false)
$identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') -Raw | ConvertFrom-Json
switch ($Action) {
    'QueryParameters' {
        $query = [ordered]@{ agent = 'Codex'; limit = 25; offset = 0 }
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'history-params.json'), ($query | ConvertTo-Json), $utf8)
    }
    'Audit' {
        & (Join-Path $PSScriptRoot 'Get-ReviewState.ps1') -OutputName 'state-after.json'
        $before = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'state-before.json') -Raw | ConvertFrom-Json
        $after = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'state-after.json') -Raw | ConvertFrom-Json
        $beforeNative = @($before.commands | Where-Object { $_.command -match '/native/vice ' }) | ConvertTo-Json -Depth 12 -Compress
        $afterNative = @($after.commands | Where-Object { $_.command -match '/native/vice ' }) | ConvertTo-Json -Depth 12 -Compress
        $beforeHashes = $before.changedNativeFileHashes | ConvertTo-Json -Compress
        $afterHashes = $after.changedNativeFileHashes | ConvertTo-Json -Compress
        $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $pinResults = foreach ($pin in $pins) {
            $hash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            [ordered]@{ path = $pin.relativePath; sha256 = $hash; expectedSha256 = $pin.sha256; bytes = (Get-Item -LiteralPath $pin.path -Force).Length; matches = ($hash -eq $pin.sha256 -and (Get-Item -LiteralPath $pin.path -Force).Length -eq $pin.bytes) }
        }
        $verdict = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.json') -Raw | ConvertFrom-Json
        $missing = foreach ($finding in $verdict.findings) { foreach ($field in @('id', 'status', 'severity', 'file', 'section', 'evidence', 'rationale', 'correction')) { if (-not $finding.PSObject.Properties[$field]) { "$($finding.id):$field" } } }
        $status = @($after.commands | Where-Object { $_.command -eq 'git -C /home/sharpninja/github/vice-sharp status --porcelain=v1 -uall' })[0]
        $outside = @($status.lines | Where-Object { $_ -notmatch '^\?\? docs/receipts/hv/20261020T122200-plan-nativerestore-001-gpt61sol/' })
        $audit = [ordered]@{ timestampLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz'); nativeReadOnlyGitEvidenceUnchanged = ($beforeNative -ceq $afterNative); eightChangedNativeFileHashesUnchanged = ($beforeHashes -ceq $afterHashes); inputCount = @($pinResults).Count; inputDriftCount = @($pinResults | Where-Object { -not $_.matches }).Count; pins = @($pinResults); dirtySuperprojectPathsOutsideReceipt = $outside; verdictMissingFields = @($missing); findingCount = @($verdict.findings).Count; uniqueFindingIdCount = @($verdict.findings.id | Sort-Object -Unique).Count; diffCheckExitCode = @($after.commands | Where-Object { $_.command.EndsWith(' diff --check') })[0].exitCode }
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'final-audit.json'), ($audit | ConvertTo-Json -Depth 15), $utf8)
        if (-not $audit.nativeReadOnlyGitEvidenceUnchanged -or -not $audit.eightChangedNativeFileHashesUnchanged -or $audit.inputDriftCount -or $outside.Count -or @($missing).Count -or $audit.diffCheckExitCode) { throw 'Final review audit has discrepancies. See final-audit.json.' }
        [ordered]@{ nativeEvidenceUnchanged = $true; trackedNativeHashesUnchanged = $true; pinsMatching = $audit.inputCount; outsideReceiptDirt = $outside.Count; diffCheckExitCode = $audit.diffCheckExitCode; findings = $audit.findingCount } | ConvertTo-Json -Compress
    }
    'RedactPrefix' {
        . '/home/sharpninja/github/mcpserver-codex-plugin/lib/marker-resolver.ps1'
        $secret = Get-MarkerField -MarkerFile '/home/sharpninja/github/vice-sharp/AGENTS-README-FIRST.yaml' -FieldName 'apiKey'
        if ([string]::IsNullOrWhiteSpace($secret)) { throw 'Cannot verify redaction without the local marker value.' }
        $raw = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'response.jsonl'))
        $raw = $raw.Substring(0, $raw.LastIndexOf("`n") + 1)
        $count = [regex]::Matches($raw, [regex]::Escape($secret)).Count
        $redacted = $raw.Replace($secret, '[REDACTED]')
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'response.redacted-prefix.jsonl'), $redacted, $utf8)
        $proof = [ordered]@{ timestampLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz'); completeResponseStream = $false; sourceIsLivePrefix = $true; rawPrefixChars = $raw.Length; credentialOccurrencesRedacted = $count; knownCredentialOccurrencesRemaining = [regex]::Matches($redacted, [regex]::Escape($secret)).Count; redactedPrefixSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'response.redacted-prefix.jsonl') -Algorithm SHA256).Hash; instruction = 'Raw response remains private and retained. Parent must redact the completed stream before sharing; this prefix is not a complete stream.' }
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'redaction-prefix-proof.json'), ($proof | ConvertTo-Json), $utf8)
        $proof | ConvertTo-Json -Compress
    }
    'Persistence' {
        $queue = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'failsafe-final.json') -Raw | ConvertFrom-Json
        $proof = [ordered]@{
            timestampLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
            reviewerAgent = 'Codex'; reviewerModel = 'gpt-6.1-sol'; reviewerEffort = 'max'
            reviewerSessionId = $identity.reviewerSessionId; reviewerRequestId = $identity.reviewerRequestId
            identityDisposition = $identity.identityDisposition
            primaryReadback = $false; primaryPersisted = $false; completedStateProvenByHistory = $false
            disposition = 'QUEUED_ONLY_NOT_PRIMARY_PERSISTED'
            primaryBeginResult = 'Rejected: bootstrap produced no sessionId; original exact error retained in response.jsonl.'
            completeTurnAttemptPath = Join-Path $PSScriptRoot 'mcp-complete-attempt.txt'
            completeTurnAttempt = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'mcp-complete-attempt.txt'))
            historyQuery = @{ method = 'workflow.sessionlog.queryHistory'; parametersPath = (Join-Path $PSScriptRoot 'history-params.json'); resultPath = (Join-Path $PSScriptRoot 'mcp-history-attempt.txt'); result = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'mcp-history-attempt.txt')); confirmsCompleted = $false }
            triageFailurePath = Join-Path $PSScriptRoot 'mcp-triage-failure.json'
            failsafeAuthorization = 'Explicit request permits full-content failsafe only after genuine primary failure including failed supported triage. Both occurred before queue creation.'
            fullContentLocalQueueProof = $queue
            localTerminalPayloadStatus = 'completed'
            serverTurnStatus = 'NOT_ESTABLISHED'
            localReadbackMethod = 'Read-McpYamlObject, concatenate ordered Markdown and JSON processingDialog chunks, ordinal compare with complete files, compare UTF8 SHA256 hashes.'
            crlfLfNormalizationApplied = $false
            rawResponsePrivate = $true
            finalAuditPath = Join-Path $PSScriptRoot 'final-audit.json'
            redactionProofPath = Join-Path $PSScriptRoot 'redaction-prefix-proof.json'
            queueReplayCount = 0
        }
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verdict.persistence.json'), ($proof | ConvertTo-Json -Depth 20) + "`n", $utf8)
        [ordered]@{ disposition = $proof.disposition; primaryPersisted = $false; completedStateProvenByHistory = $false; fullContentLocalReadbackMatches = $queue.localReadbackMatches; crlfLfNormalizationApplied = $false } | ConvertTo-Json -Compress
    }
}
