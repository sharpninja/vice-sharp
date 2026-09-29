#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ReceiptDirectory,

    [string]$WorkspacePath = (Split-Path -Parent $PSScriptRoot),

    [string[]]$HistoricalRefs = @('d28493a', 'd1f7175')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-NormalizedText {
    param([AllowEmptyString()][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ''
    }

    return (($Text -replace "`r`n", "`n" -replace "`r", "`n").Trim())
}

function ConvertTo-Priority {
    param([AllowEmptyString()][string]$Priority)

    $value = (ConvertTo-NormalizedText $Priority).ToLowerInvariant()
    if ($value -match '^p0\b|critical|highest') { return 'high' }
    if ($value -match '^p1\b|important|medium') { return 'medium' }
    if ($value -match '^p2\b|low') { return 'low' }
    if ($value -in @('high', 'medium', 'low')) { return $value }
    return 'medium'
}

function Get-MarkdownBlock {
    param(
        [string[]]$Lines,
        [int]$Start,
        [int]$End,
        [string[]]$HeadingNames
    )

    $namePattern = (($HeadingNames | ForEach-Object { [regex]::Escape($_) }) -join '|')
    $headingPattern = '^(?:#{2,3}\s+(?:' + $namePattern + ')\s*|\*\*(?:' + $namePattern + '):\*\*\s*(?<inline>.*))$'
    $headingIndex = -1
    $inlineValue = ''
    for ($index = $Start; $index -lt $End; $index++) {
        if ($Lines[$index] -match $headingPattern) {
            $headingIndex = $index
            $inlineValue = if ($Matches.ContainsKey('inline')) { [string]$Matches['inline'] } else { '' }
            break
        }
    }

    if ($headingIndex -lt 0) { return '' }

    $block = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($inlineValue)) { [void]$block.Add($inlineValue) }
    for ($index = $headingIndex + 1; $index -lt $End; $index++) {
        if ($Lines[$index] -match '^#{2,3}\s+') { break }
        if ($Lines[$index] -match '^\*\*[A-Za-z][^*]*:\*\*') { break }
        [void]$block.Add($Lines[$index])
    }

    return (ConvertTo-NormalizedText ($block -join "`n"))
}

function ConvertFrom-AcBlock {
    param([AllowEmptyString()][string]$Block)

    if ([string]::IsNullOrWhiteSpace($Block)) { return @() }

    $criteria = [System.Collections.Generic.List[string]]::new()
    $current = ''
    foreach ($line in ($Block -split "`n")) {
        if ($line -match '^\s*(?:\d+[.)]|[-*])\s+(?:\[[ xX]\]\s*)?(?<text>.+?)\s*$') {
            if (-not [string]::IsNullOrWhiteSpace($current)) {
                [void]$criteria.Add((ConvertTo-NormalizedText $current))
            }
            $current = $Matches.text
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($line) -and -not [string]::IsNullOrWhiteSpace($current)) {
            $current = "$current $($line.Trim())"
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($current)) {
        [void]$criteria.Add((ConvertTo-NormalizedText $current))
    }

    if ($criteria.Count -eq 0 -and -not [string]::IsNullOrWhiteSpace($Block)) {
        [void]$criteria.Add((ConvertTo-NormalizedText $Block))
    }

    return @($criteria | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-FieldValue {
    param(
        [string[]]$Lines,
        [int]$Start,
        [int]$End,
        [string]$Name
    )

    $pattern = '^\*\*' + [regex]::Escape($Name) + ':\*\*\s*(?<value>.*?)\s*$'
    for ($index = $Start; $index -lt $End; $index++) {
        if ($Lines[$index] -match $pattern) {
            return (ConvertTo-NormalizedText $Matches.value)
        }
    }
    return ''
}

function Get-RequirementKindFromPath {
    param([string]$Path)

    $normalized = $Path.Replace('\', '/').ToLowerInvariant()
    if ($normalized -match '/functional/') { return 'fr' }
    if ($normalized -match '/technical/') { return 'tr' }
    if ($normalized -match '/test/') { return 'test' }
    throw "Cannot infer requirement kind from path '$Path'."
}

function ConvertFrom-RequirementMarkdown {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$SourceLabel,
        [Parameter(Mandatory)][ValidateSet('fr', 'tr', 'test')][string]$Kind
    )

    $normalized = $Text -replace "`r`n", "`n" -replace "`r", "`n"
    $lines = @($normalized -split "`n")
    $idIndexes = [System.Collections.Generic.List[int]]::new()
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match '^\*\*ID:\*\*\s*(?<id>[A-Z][A-Z0-9-]*-\d+)\s*$') {
            [void]$idIndexes.Add($index)
        }
    }

    $records = [System.Collections.Generic.List[object]]::new()
    for ($ordinal = 0; $ordinal -lt $idIndexes.Count; $ordinal++) {
        $start = $idIndexes[$ordinal]
        $end = if ($ordinal + 1 -lt $idIndexes.Count) { $idIndexes[$ordinal + 1] } else { $lines.Count }
        $id = ([regex]::Match($lines[$start], '^\*\*ID:\*\*\s*(?<id>[A-Z][A-Z0-9-]*-\d+)\s*$')).Groups['id'].Value
        $title = Get-FieldValue -Lines $lines -Start $start -End $end -Name 'Title'
        if ([string]::IsNullOrWhiteSpace($title)) {
            for ($heading = $start - 1; $heading -ge 0 -and $heading -ge ($start - 8); $heading--) {
                if ($lines[$heading] -match '^##\s+[^:]+:\s*(?<title>.+?)\s*$') {
                    $title = ConvertTo-NormalizedText $Matches.title
                    break
                }
            }
        }
        if ([string]::IsNullOrWhiteSpace($title)) { $title = $id }

        $priorityRaw = Get-FieldValue -Lines $lines -Start $start -End $end -Name 'Priority'
        $bodyHeadings = if ($Kind -eq 'test') { @('Condition', 'Description') } else { @('Description') }
        $body = Get-MarkdownBlock -Lines $lines -Start $start -End $end -HeadingNames $bodyHeadings
        $acBlock = Get-MarkdownBlock -Lines $lines -Start $start -End $end -HeadingNames @('Acceptance Criteria')
        $acceptanceCriteria = @(ConvertFrom-AcBlock -Block $acBlock)
        $traceability = Get-MarkdownBlock -Lines $lines -Start $start -End $end -HeadingNames @('Traceability')

        [void]$records.Add([pscustomobject][ordered]@{
            Key = "$Kind|$id"
            Kind = $Kind
            Id = $id
            Title = $title
            Body = $body
            Priority = ConvertTo-Priority $priorityRaw
            PriorityRaw = $priorityRaw
            AcceptanceCriteria = $acceptanceCriteria
            Traceability = $traceability
            SourceLabel = $SourceLabel
            SourcePath = $SourcePath
            SourceLine = $start + 1
        })
    }

    return @($records)
}

function Get-CurrentCanonicalRecords {
    param([string]$Root)

    $records = [System.Collections.Generic.List[object]]::new()
    foreach ($directory in @('functional', 'technical', 'test')) {
        $path = Join-Path $Root "docs/requirements/$directory"
        foreach ($file in Get-ChildItem -LiteralPath $path -Recurse -File -Filter '*.md' | Sort-Object FullName) {
            $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
            $text = [IO.File]::ReadAllText($file.FullName)
            $kind = Get-RequirementKindFromPath $file.FullName
            foreach ($record in ConvertFrom-RequirementMarkdown -Text $text -SourcePath $relative -SourceLabel 'current-canonical' -Kind $kind) {
                [void]$records.Add($record)
            }
        }
    }
    return @($records)
}

function Get-HistoricalRecords {
    param(
        [string]$Root,
        [string]$Ref
    )

    $records = [System.Collections.Generic.List[object]]::new()
    $paths = @(& git -C $Root ls-tree -r --name-only $Ref -- docs/requirements/functional docs/requirements/technical docs/requirements/test)
    if ($LASTEXITCODE -ne 0) { throw "git ls-tree failed for $Ref." }
    foreach ($path in $paths | Where-Object { $_ -match '\.md$' }) {
        $text = (& git -C $Root show "$Ref`:$path" | Out-String)
        if ($LASTEXITCODE -ne 0) { throw "git show failed for $Ref`:$path." }
        $kind = Get-RequirementKindFromPath $path
        foreach ($record in ConvertFrom-RequirementMarkdown -Text $text -SourcePath "$Ref`:$path" -SourceLabel "git-$Ref" -Kind $kind) {
            [void]$records.Add($record)
        }
    }
    return @($records)
}

function Get-InlineAcceptanceEvidence {
    param(
        [string]$Root,
        [string[]]$Ids
    )

    $result = @{}
    if ($Ids.Count -eq 0) { return $result }
    $idPattern = (($Ids | Sort-Object -Unique | ForEach-Object { [regex]::Escape($_) }) -join '|')
    $regex = [regex]::new("(?i)\b(?<id>$idPattern)\b.*?\bAcceptance\s*:\s*(?<ac>.+)$", [Text.RegularExpressions.RegexOptions]::Compiled)
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Root 'docs') -Recurse -File -Filter '*.md' | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        $lineNumber = 0
        foreach ($line in [IO.File]::ReadLines($file.FullName)) {
            $lineNumber++
            $match = $regex.Match($line)
            if (-not $match.Success) { continue }
            $id = $match.Groups['id'].Value.ToUpperInvariant()
            if ($result.ContainsKey($id)) { continue }
            $text = $match.Groups['ac'].Value
            $text = ($text -replace '\s+->\s+(?:TR|TEST)-[A-Z0-9-]+.*$', '').Trim()
            $criteria = @((ConvertTo-NormalizedText $text))
            if ($criteria.Count -eq 0) { continue }
            $result[$id] = [pscustomobject][ordered]@{
                AcceptanceCriteria = $criteria
                SourcePath = $relative
                SourceLine = $lineNumber
                RawLine = $line.Trim()
            }
        }
    }
    return $result
}

function Get-StructuredAcceptanceEvidence {
    param(
        [string]$Root,
        [string[]]$Ids
    )

    $result = @{}
    if ($Ids.Count -eq 0) { return $result }
    $idPattern = (($Ids | Sort-Object -Unique | ForEach-Object { [regex]::Escape($_) }) -join '|')
    $headingRegex = [regex]::new("(?i)(?<id>$idPattern)", [Text.RegularExpressions.RegexOptions]::Compiled)
    $otherFrRegex = [regex]::new('(?i)\bFR-[A-Z0-9-]+-\d+\b', [Text.RegularExpressions.RegexOptions]::Compiled)

    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Root 'docs') -Recurse -File -Filter '*.md' | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        $lines = @([IO.File]::ReadAllLines($file.FullName))
        for ($index = 0; $index -lt $lines.Count; $index++) {
            $headingMatch = $headingRegex.Match($lines[$index])
            if (-not $headingMatch.Success) { continue }
            $id = $headingMatch.Groups['id'].Value.ToUpperInvariant()
            if ($result.ContainsKey($id)) { continue }
            $criteria = [System.Collections.Generic.List[string]]::new()
            for ($scan = $index + 1; $scan -lt [Math]::Min($lines.Count, $index + 60); $scan++) {
                if ($scan -gt $index + 1 -and ($lines[$scan] -match '^#{1,3}\s+' -or $lines[$scan] -match '^\*\*FR-[A-Z0-9-]+-\d+')) { break }
                $otherMatch = $otherFrRegex.Match($lines[$scan])
                if ($otherMatch.Success -and $otherMatch.Value -ne $id -and $lines[$scan] -match '^\s*(?:[-*]|\*\*)') { break }
                if ($lines[$scan] -match '^\s*-\s+AC-[A-Z0-9*-]+:\s*(?<text>.+?)\s*$') {
                    $text = ConvertTo-NormalizedText $Matches.text
                    if (-not [string]::IsNullOrWhiteSpace($text)) { [void]$criteria.Add($text) }
                }
            }
            if ($criteria.Count -gt 0) {
                $result[$id] = [pscustomobject][ordered]@{
                    AcceptanceCriteria = @($criteria)
                    SourcePath = $relative
                    SourceLine = $index + 1
                }
            }
        }
    }
    return $result
}

function Get-InlineMappingEvidence {
    param(
        [string]$Root,
        [string[]]$Ids
    )

    $result = @{}
    if ($Ids.Count -eq 0) { return $result }
    $idPattern = (($Ids | Sort-Object -Unique | ForEach-Object { [regex]::Escape($_) }) -join '|')
    $frRegex = [regex]::new("(?i)\b(?<id>$idPattern)\b", [Text.RegularExpressions.RegexOptions]::Compiled)
    $trRegex = [regex]::new('\bTR-[A-Z0-9-]+-\d+\b', [Text.RegularExpressions.RegexOptions]::Compiled)
    $testRegex = [regex]::new('\bTEST-[A-Z0-9-]+(?:-\d+|-[A-Z][A-Z0-9-]*)\b', [Text.RegularExpressions.RegexOptions]::Compiled)

    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Root 'docs') -Recurse -File -Filter '*.md' | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        $lineNumber = 0
        foreach ($line in [IO.File]::ReadLines($file.FullName)) {
            $lineNumber++
            if ($line -notmatch '->') { continue }
            $frMatches = @($frRegex.Matches($line))
            if ($frMatches.Count -eq 0) { continue }
            $trIds = @($trRegex.Matches($line) | ForEach-Object Value | Sort-Object -Unique)
            $testIds = @($testRegex.Matches($line) | ForEach-Object Value | Sort-Object -Unique)
            if ($trIds.Count -eq 0 -and $testIds.Count -eq 0) { continue }
            foreach ($frMatch in $frMatches) {
                $id = $frMatch.Groups['id'].Value.ToUpperInvariant()
                if (-not $result.ContainsKey($id)) {
                    $result[$id] = [pscustomobject][ordered]@{
                        TrIds = [System.Collections.Generic.List[string]]::new()
                        TestIds = [System.Collections.Generic.List[string]]::new()
                        Evidence = [System.Collections.Generic.List[object]]::new()
                    }
                }
                foreach ($trId in $trIds) {
                    if (-not $result[$id].TrIds.Contains($trId)) { [void]$result[$id].TrIds.Add($trId) }
                }
                foreach ($testId in $testIds) {
                    if (-not $result[$id].TestIds.Contains($testId)) { [void]$result[$id].TestIds.Add($testId) }
                }
                [void]$result[$id].Evidence.Add([pscustomobject][ordered]@{
                    SourcePath = $relative
                    SourceLine = $lineNumber
                    RawLine = $line.Trim()
                })
            }
        }
    }
    return $result
}

function Get-RequirementIds {
    param(
        [AllowEmptyString()][string]$Text,
        [ValidateSet('FR', 'TR', 'TEST')][string]$Prefix
    )

    if ([string]::IsNullOrWhiteSpace($Text)) { return @() }
    $pattern = if ($Prefix -eq 'TEST') {
        '\bTEST-[A-Z0-9-]+(?:-\d+|-[A-Z][A-Z0-9-]*)\b'
    } else {
        "\b$Prefix-[A-Z0-9-]+-\d+\b"
    }
    return @([regex]::Matches($Text, $pattern) | ForEach-Object Value | Sort-Object -Unique)
}

function Get-RequirementAreas {
    param(
        [AllowEmptyString()][string]$Traceability,
        [ValidateSet('FR', 'TR')][string]$Prefix
    )

    if ([string]::IsNullOrWhiteSpace($Traceability)) { return @() }
    $result = [System.Collections.Generic.List[string]]::new()
    $label = if ($Prefix -eq 'FR') { 'Related FR Area\(s\)' } else { 'Related TR Area\(s\)' }
    foreach ($line in ($Traceability -split "`n")) {
        if ($line -notmatch "(?i)^\s*-\s*\*\*${label}:\*\*\s*(?<value>.+)$") { continue }
        foreach ($match in [regex]::Matches($Matches.value, "\b$Prefix-[A-Z0-9-]+\b")) {
            if (-not $result.Contains($match.Value)) { [void]$result.Add($match.Value) }
        }
    }
    return @($result)
}

function ConvertFrom-LiveRequirement {
    param(
        [Parameter(Mandatory)][object]$Record,
        [Parameter(Mandatory)][ValidateSet('fr', 'tr', 'test')][string]$Kind
    )

    $body = if ($Kind -eq 'test') { $Record.Condition } else { $Record.Body }
    $criteria = @($Record.AcceptanceCriteria | ForEach-Object {
        if ($_ -is [string]) { ConvertTo-NormalizedText $_ }
        elseif ($null -ne $_.text) { ConvertTo-NormalizedText ([string]$_.text) }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    return [pscustomobject][ordered]@{
        Key = "$Kind|$($Record.Id)"
        Kind = $Kind
        Id = [string]$Record.Id
        Title = if ([string]::IsNullOrWhiteSpace([string]$Record.Title)) { [string]$Record.Id } else { [string]$Record.Title }
        Body = ConvertTo-NormalizedText ([string]$body)
        Priority = ConvertTo-Priority ([string]$Record.Priority)
        Status = [string]$Record.Status
        Notes = $Record.Notes
        AcceptanceCriteria = $criteria
        AcceptanceCriteriaRaw = @($Record.AcceptanceCriteria)
        ScopeStartLayerKey = $Record.ScopeStartLayerKey
        ScopeEndLayerKey = $Record.ScopeEndLayerKey
    }
}

function New-InferredAcceptanceCriteria {
    param(
        [Parameter(Mandatory)][object]$Record
    )

    $criteria = [System.Collections.Generic.List[string]]::new()
    $body = ConvertTo-NormalizedText ([string]$Record.Body)
    if (-not [string]::IsNullOrWhiteSpace($body)) {
        $protectedBody = ($body -replace '\be\.g\.', 'e_g_' -replace '\bi\.e\.', 'i_e_')
        $sentences = @([regex]::Split(($protectedBody -replace "`n", ' '), '(?<=[.!?])\s+') | ForEach-Object { ($_ -replace 'e_g_', 'e.g.' -replace 'i_e_', 'i.e.').Trim() } | Where-Object {
            ($_.Length -ge 20) -and
            ($_ -notmatch '^(?i:VICE\s+\S+\.(?:c|h)|Closes\s+|DONE\.?$)') -and
            ($_ -notmatch '(?i)^(?:Source|Reference)s?\s*:')
        })
        foreach ($sentence in $sentences | Select-Object -First 6) {
            [void]$criteria.Add($sentence)
        }
    }

    if ($criteria.Count -eq 0) {
        [void]$criteria.Add("A deterministic validation demonstrates the behavior named '$($Record.Title)' under its supported configuration and boundary conditions.")
    }

    [void]$criteria.Add('At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.')
    $unique = [System.Collections.Generic.List[string]]::new()
    foreach ($criterion in $criteria) {
        if (-not $unique.Contains($criterion)) { [void]$unique.Add($criterion) }
    }
    return @($unique)
}

function Test-RecordDifference {
    param(
        [AllowNull()][object]$Live,
        [Parameter(Mandatory)][object]$Proposed
    )

    if ($null -eq $Live) { return $true }
    if ((ConvertTo-NormalizedText $Live.Title) -cne (ConvertTo-NormalizedText $Proposed.Title)) { return $true }
    if ((ConvertTo-NormalizedText $Live.Body) -cne (ConvertTo-NormalizedText $Proposed.Body)) { return $true }
    if ($Live.Priority -cne $Proposed.Priority) { return $true }
    if ($Live.Status -cne $Proposed.Status) { return $true }
    if ([string]$Live.ScopeStartLayerKey -cne [string]$Proposed.ScopeStartLayerKey) { return $true }
    if ([string]$Live.ScopeEndLayerKey -cne [string]$Proposed.ScopeEndLayerKey) { return $true }
    $liveAc = @($Live.AcceptanceCriteria | ForEach-Object { ConvertTo-NormalizedText $_ })
    $proposedAc = @($Proposed.AcceptanceCriteria | ForEach-Object { ConvertTo-NormalizedText $_ })
    if (($liveAc | ConvertTo-Json -Compress) -cne ($proposedAc | ConvertTo-Json -Compress)) { return $true }
    return $false
}

$workspaceFull = (Resolve-Path -LiteralPath $WorkspacePath).ProviderPath
$receiptFull = (Resolve-Path -LiteralPath $ReceiptDirectory).ProviderPath
$livePath = Join-Path $receiptFull 'live-raw-requirements.json'
if (-not (Test-Path -LiteralPath $livePath -PathType Leaf)) {
    throw "Missing required preflight input: $livePath"
}

$livePayload = [IO.File]::ReadAllText($livePath) | ConvertFrom-Json
$liveRecords = [System.Collections.Generic.List[object]]::new()
foreach ($record in @($livePayload.functional)) { [void]$liveRecords.Add((ConvertFrom-LiveRequirement -Record $record -Kind 'fr')) }
foreach ($record in @($livePayload.technical)) { [void]$liveRecords.Add((ConvertFrom-LiveRequirement -Record $record -Kind 'tr')) }
foreach ($record in @($livePayload.testing)) { [void]$liveRecords.Add((ConvertFrom-LiveRequirement -Record $record -Kind 'test')) }

$currentRecords = @(Get-CurrentCanonicalRecords -Root $workspaceFull)
$historicalByRef = [ordered]@{}
foreach ($ref in $HistoricalRefs) {
    $historicalByRef[$ref] = @(Get-HistoricalRecords -Root $workspaceFull -Ref $ref)
}

$liveByKey = @{}
foreach ($record in $liveRecords) { $liveByKey[$record.Key] = $record }
$currentByKey = @{}
foreach ($record in $currentRecords) {
    if ($currentByKey.ContainsKey($record.Key)) { throw "Duplicate current canonical typed key $($record.Key)." }
    $currentByKey[$record.Key] = $record
}
$historicalByKey = @{}
foreach ($ref in $HistoricalRefs) {
    foreach ($record in $historicalByRef[$ref]) {
        if (-not $historicalByKey.ContainsKey($record.Key)) { $historicalByKey[$record.Key] = $record }
    }
}

$planRecoveredRecords = @(
    [pscustomobject][ordered]@{
        Key = 'test|TEST-ROMM-DETAIL-001'; Kind = 'test'; Id = 'TEST-ROMM-DETAIL-001'; Title = 'RomM ROM detail validation';
        Body = 'RomMGateway detail mapping and RomDetailViewModel add-to-collection behavior are proven by deterministic automated tests for files, cover, summary, launchability, and collection mutation.';
        Priority = 'medium'; PriorityRaw = 'Recovered from approved BDPv4 plan'; AcceptanceCriteria = @(); Traceability = 'FR-ROMM-DETAIL-001';
        SourceLabel = 'approved-plan-recovery'; SourcePath = 'docs/plans/PLAN-ROMM-BDPv4.md'; SourceLine = 172
    },
    [pscustomobject][ordered]@{
        Key = 'test|TEST-ROMM-AVUI-001'; Kind = 'test'; Id = 'TEST-ROMM-AVUI-001'; Title = 'RomM Avalonia UI validation';
        Body = 'Build, headless view-model, and end-to-end validation prove LibraryView and ListsView binding, connection, browse, search, list, collection, and launch behavior.';
        Priority = 'medium'; PriorityRaw = 'Recovered from approved BDPv4 plan'; AcceptanceCriteria = @(); Traceability = 'FR-ROMM-AVUI-001';
        SourceLabel = 'approved-plan-recovery'; SourcePath = 'docs/plans/PLAN-ROMM-BDPv4.md'; SourceLine = 246
    },
    [pscustomobject][ordered]@{
        Key = 'test|TEST-ROMM-PKG-001'; Kind = 'test'; Id = 'TEST-ROMM-PKG-001'; Title = 'RomM NuGet package validation';
        Body = 'Package metadata and pack-output tests prove both RomM libraries are packable with the approved package ids, GPL-2.0-or-later metadata, and exact dependency graph.';
        Priority = 'medium'; PriorityRaw = 'Recovered from approved BDPv4 plan'; AcceptanceCriteria = @(); Traceability = 'FR-ROMM-PKG-001';
        SourceLabel = 'approved-plan-recovery'; SourcePath = 'docs/plans/PLAN-ROMM-BDPv4.md'; SourceLine = 255
    }
)
$planRecoveredByKey = @{}
foreach ($record in $planRecoveredRecords) { $planRecoveredByKey[$record.Key] = $record }

$allKeys = @($liveByKey.Keys + $currentByKey.Keys + $historicalByKey.Keys + $planRecoveredByKey.Keys | Sort-Object -Unique)
$functionalIds = @($allKeys | Where-Object { $_ -like 'fr|*' } | ForEach-Object { $_.Substring(3) })
$inlineAcceptance = Get-InlineAcceptanceEvidence -Root $workspaceFull -Ids $functionalIds
$structuredAcceptance = Get-StructuredAcceptanceEvidence -Root $workspaceFull -Ids $functionalIds
$inlineMapping = Get-InlineMappingEvidence -Root $workspaceFull -Ids $functionalIds

$ledger = [System.Collections.Generic.List[object]]::new()
$operations = [System.Collections.Generic.List[object]]::new()
$rollback = [System.Collections.Generic.List[object]]::new()
$invariants = [System.Collections.Generic.List[object]]::new()

foreach ($key in $allKeys) {
    $live = if ($liveByKey.ContainsKey($key)) { $liveByKey[$key] } else { $null }
    $current = if ($currentByKey.ContainsKey($key)) { $currentByKey[$key] } else { $null }
    $historical = if ($historicalByKey.ContainsKey($key)) { $historicalByKey[$key] } else { $null }
    $planRecovered = if ($planRecoveredByKey.ContainsKey($key)) { $planRecoveredByKey[$key] } else { $null }
    $kind, $id = $key -split '\|', 2

    $authoritative = if ($null -ne $current) { $current } elseif ($null -ne $historical) { $historical } elseif ($null -ne $planRecovered) { $planRecovered } else { $live }
    $sourceClass = if ($null -ne $current) { 'current-canonical' } elseif ($null -ne $historical) { $historical.SourceLabel } elseif ($null -ne $planRecovered) { $planRecovered.SourceLabel } else { 'live-only' }
    $criteria = @($authoritative.AcceptanceCriteria)
    $acSourceClass = if ($criteria.Count -gt 0) { $sourceClass } else { 'none' }
    $acSourcePath = if ($null -ne $authoritative.PSObject.Properties['SourcePath']) { [string]$authoritative.SourcePath } else { '' }
    $acSourceLine = if ($null -ne $authoritative.PSObject.Properties['SourceLine']) { $authoritative.SourceLine } else { $null }
    $acInference = $false

    if ($kind -eq 'fr' -and $criteria.Count -eq 0 -and $inlineAcceptance.ContainsKey($id)) {
        $evidence = $inlineAcceptance[$id]
        $criteria = @($evidence.AcceptanceCriteria)
        $acSourceClass = 'plan-or-audit-inline-acceptance'
        $acSourcePath = $evidence.SourcePath
        $acSourceLine = $evidence.SourceLine
    }

    if ($kind -eq 'fr' -and $criteria.Count -eq 0 -and $structuredAcceptance.ContainsKey($id)) {
        $evidence = $structuredAcceptance[$id]
        $criteria = @($evidence.AcceptanceCriteria)
        $acSourceClass = 'plan-or-audit-structured-acceptance'
        $acSourcePath = $evidence.SourcePath
        $acSourceLine = $evidence.SourceLine
    }

    if ($kind -eq 'fr' -and $criteria.Count -eq 0) {
        $criteria = @(New-InferredAcceptanceCriteria -Record $authoritative)
        $acSourceClass = 'inferred-from-surviving-fr-body'
        $acSourcePath = ''
        $acSourceLine = $null
        $acInference = $true
    }

    $status = if ($null -ne $live -and -not [string]::IsNullOrWhiteSpace($live.Status)) { $live.Status } else { 'pending' }
    if ($null -eq $live) { $status = 'pending' }

    $proposed = [pscustomobject][ordered]@{
        Kind = $kind
        Id = $id
        Title = $authoritative.Title
        Body = $authoritative.Body
        Priority = $authoritative.Priority
        Status = $status
        Notes = if ($null -ne $live) { $live.Notes } else { $null }
        AcceptanceCriteria = @($criteria)
        ScopeStartLayerKey = if ($null -ne $live) { $live.ScopeStartLayerKey } else { 'layer-1' }
        ScopeEndLayerKey = if ($null -ne $live) { $live.ScopeEndLayerKey } else { $null }
    }

    $classification = if ($null -eq $live) { 'create' } elseif (Test-RecordDifference -Live $live -Proposed $proposed) { 'update' } else { 'preserve' }
    $legacy = $id -match 'XBOX|^FR-X|^TR-X|^TEST-X|ROMM-XBOX|GAMEPAD|SYSBTN|^FR-CTX-'
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($kind -eq 'fr' -and $criteria.Count -eq 0) { [void]$issues.Add('FR has no acceptance criteria.') }
    if ($kind -eq 'fr' -and $acInference) { [void]$issues.Add('Acceptance criteria are inferred from the surviving FR body and require owner review.') }
    if ([string]::IsNullOrWhiteSpace($proposed.Body)) { [void]$issues.Add('Requirement body is empty.') }
    if ($id -eq 'TEST-VIC-001' -and $kind -eq 'tr') { [void]$issues.Add('Known wrong-type placeholder; typed identity repair required.') }

    [void]$ledger.Add([pscustomobject][ordered]@{
        Key = $key
        Kind = $kind
        Id = $id
        IsLegacy = $legacy
        CurrentCanonicalPresent = $null -ne $current
        HistoricalPresent = $null -ne $historical
        PlanAuditRecoveredPresent = $null -ne $planRecovered
        LivePresent = $null -ne $live
        SourceClass = $sourceClass
        AcSourceClass = $acSourceClass
        AcSourcePath = $acSourcePath
        AcSourceLine = $acSourceLine
        AcInferred = $acInference
        Classification = $classification
        Issues = @($issues)
        Live = $live
        Proposed = $proposed
    })

    if ($classification -in @('create', 'update')) {
        [void]$operations.Add([pscustomobject][ordered]@{
            Order = $operations.Count + 1
            Operation = $classification
            Kind = $kind
            Id = $id
            Before = $live
            After = $proposed
            Evidence = [pscustomobject][ordered]@{
                SourceClass = $sourceClass
                AcceptanceCriteriaSourceClass = $acSourceClass
                SourcePath = $acSourcePath
                SourceLine = $acSourceLine
                InferredAcceptanceCriteria = $acInference
            }
        })

        $inverse = if ($classification -eq 'create') {
            [pscustomobject][ordered]@{ Operation = 'delete'; Kind = $kind; Id = $id; Restore = $null }
        } else {
            [pscustomobject][ordered]@{ Operation = 'update'; Kind = $kind; Id = $id; Restore = $live }
        }
        [void]$rollback.Add([pscustomobject][ordered]@{
            ReverseOrder = 0
            ForwardOrder = $operations.Count
            Inverse = $inverse
        })
    }
}

$functionalLedger = @($ledger | Where-Object { $_.Kind -eq 'fr' })
$knownTrIds = @($ledger | Where-Object Kind -eq 'tr' | ForEach-Object Id | Sort-Object -Unique)
$knownTestIds = @($ledger | Where-Object Kind -eq 'test' | ForEach-Object Id | Sort-Object -Unique)
$mappingByFr = @{}
foreach ($record in $functionalLedger) {
    $mappingByFr[$record.Id] = [pscustomobject][ordered]@{
        FrId = $record.Id
        TrIds = [System.Collections.Generic.List[string]]::new()
        TestIds = [System.Collections.Generic.List[string]]::new()
        Evidence = [System.Collections.Generic.List[object]]::new()
    }
}

$liveMappingByFr = @{}
foreach ($mapping in @($livePayload.mapping)) {
    $frId = [string]$mapping.FrId
    $liveMappingByFr[$frId] = [pscustomobject][ordered]@{
        FrId = $frId
        TrIds = @($mapping.TrIds | Sort-Object -Unique)
        TestIds = @($mapping.TestIds | Sort-Object -Unique)
    }
    if (-not $mappingByFr.ContainsKey($frId)) { continue }
    foreach ($trId in @($mapping.TrIds)) {
        if (-not $mappingByFr[$frId].TrIds.Contains($trId)) { [void]$mappingByFr[$frId].TrIds.Add($trId) }
    }
    foreach ($testId in @($mapping.TestIds)) {
        if (-not $mappingByFr[$frId].TestIds.Contains($testId)) { [void]$mappingByFr[$frId].TestIds.Add($testId) }
    }
    [void]$mappingByFr[$frId].Evidence.Add([pscustomobject][ordered]@{ Source = 'live-mcp-mapping'; Detail = 'Preserved existing edge set.' })
}

foreach ($record in $functionalLedger) {
    $source = if ($currentByKey.ContainsKey("fr|$($record.Id)")) { $currentByKey["fr|$($record.Id)"] } elseif ($historicalByKey.ContainsKey("fr|$($record.Id)")) { $historicalByKey["fr|$($record.Id)"] } else { $null }
    if ($null -ne $source -and $null -ne $source.PSObject.Properties['Traceability']) {
        foreach ($trId in Get-RequirementIds -Text $source.Traceability -Prefix TR) {
            if ($trId -in $knownTrIds -and -not $mappingByFr[$record.Id].TrIds.Contains($trId)) { [void]$mappingByFr[$record.Id].TrIds.Add($trId) }
        }
        foreach ($testId in Get-RequirementIds -Text $source.Traceability -Prefix TEST) {
            if ($testId -in $knownTestIds -and -not $mappingByFr[$record.Id].TestIds.Contains($testId)) { [void]$mappingByFr[$record.Id].TestIds.Add($testId) }
        }
        if (@(Get-RequirementIds -Text $source.Traceability -Prefix TR).Count -gt 0 -or @(Get-RequirementIds -Text $source.Traceability -Prefix TEST).Count -gt 0) {
            [void]$mappingByFr[$record.Id].Evidence.Add([pscustomobject][ordered]@{ Source = $source.SourceLabel; Detail = "$($source.SourcePath):$($source.SourceLine) traceability block." })
        }
    }

    if ($inlineMapping.ContainsKey($record.Id)) {
        foreach ($trId in @($inlineMapping[$record.Id].TrIds)) {
            if ($trId -in $knownTrIds -and -not $mappingByFr[$record.Id].TrIds.Contains($trId)) { [void]$mappingByFr[$record.Id].TrIds.Add($trId) }
        }
        foreach ($testId in @($inlineMapping[$record.Id].TestIds)) {
            if ($testId -in $knownTestIds -and -not $mappingByFr[$record.Id].TestIds.Contains($testId)) { [void]$mappingByFr[$record.Id].TestIds.Add($testId) }
        }
        foreach ($evidence in @($inlineMapping[$record.Id].Evidence)) {
            [void]$mappingByFr[$record.Id].Evidence.Add([pscustomobject][ordered]@{ Source = 'plan-or-audit-inline-mapping'; Detail = "$($evidence.SourcePath):$($evidence.SourceLine)" })
        }
    }
}

$testSources = @($currentRecords | Where-Object Kind -eq 'test')
foreach ($historicalRecord in @($historicalByKey.Values | Where-Object Kind -eq 'test')) {
    if (-not ($testSources | Where-Object Key -eq $historicalRecord.Key)) { $testSources += $historicalRecord }
}
foreach ($planRecord in @($planRecoveredRecords | Where-Object Kind -eq 'test')) {
    if (-not ($testSources | Where-Object Key -eq $planRecord.Key)) { $testSources += $planRecord }
}
foreach ($testRecord in $testSources) {
    $traceability = [string]$testRecord.Traceability
    $exactFrIds = @(Get-RequirementIds -Text $traceability -Prefix FR)
    $frAreas = @(Get-RequirementAreas -Traceability $traceability -Prefix FR)
    $relatedTrIds = @(Get-RequirementIds -Text $traceability -Prefix TR)
    $targets = [System.Collections.Generic.List[string]]::new()
    foreach ($frId in $exactFrIds) {
        if ($mappingByFr.ContainsKey($frId) -and -not $targets.Contains($frId)) { [void]$targets.Add($frId) }
    }
    foreach ($area in $frAreas) {
        foreach ($frId in $functionalLedger.Id | Where-Object { $_ -eq $area -or $_ -like "$area-*" }) {
            if (-not $targets.Contains($frId)) { [void]$targets.Add($frId) }
        }
    }
    foreach ($frId in $targets) {
        if (-not $mappingByFr[$frId].TestIds.Contains($testRecord.Id)) { [void]$mappingByFr[$frId].TestIds.Add($testRecord.Id) }
        foreach ($trId in $relatedTrIds) {
            if ($trId -in $knownTrIds -and -not $mappingByFr[$frId].TrIds.Contains($trId)) { [void]$mappingByFr[$frId].TrIds.Add($trId) }
        }
        [void]$mappingByFr[$frId].Evidence.Add([pscustomobject][ordered]@{ Source = $testRecord.SourceLabel; Detail = "$($testRecord.SourcePath):$($testRecord.SourceLine) maps $($testRecord.Id) by explicit FR id/area traceability." })
    }
}

$domainFallbackRules = @(
    [pscustomobject]@{ Pattern = '^FR-(CPU|CIA|CRT|DRV|INP|MEM|TAP|VIA|VIC)-'; TrIds = @('TR-CYCLE-001'); TestIds = @() },
    [pscustomobject]@{ Pattern = '^FR-(D71|D81|DRV1540|DRV1541II|DRVMODEL)-'; TrIds = @('TR-DRV-EDGE-001'); TestIds = @() },
    [pscustomobject]@{ Pattern = '^FR-DEVCARDART-'; TrIds = @('TR-UI-DEVART-001'); TestIds = @('TEST-UI-DEVCARDART-001') },
    [pscustomobject]@{ Pattern = '^FR-MED-'; TrIds = @('TR-MEDIA-001'); TestIds = @() },
    [pscustomobject]@{ Pattern = '^FR-MON-'; TrIds = @('TR-LIB-001'); TestIds = @() },
    [pscustomobject]@{ Pattern = '^FR-PRF-'; TrIds = @('TR-DET-001'); TestIds = @() },
    [pscustomobject]@{ Pattern = '^FR-SNP-'; TrIds = @('TR-STATE-001'); TestIds = @() },
    [pscustomobject]@{ Pattern = '^FR-(UIFLYOUT|UIMENUBAR|UIPERIPHERAL|UISETTINGS)-'; TrIds = @('TR-UI-SHELL-001'); TestIds = @() },
    [pscustomobject]@{ Pattern = '^FR-UIFLYOUT-001$'; TrIds = @(); TestIds = @('TEST-UIFLYOUT-001') },
    [pscustomobject]@{ Pattern = '^FR-UIMENUBAR-001$'; TrIds = @(); TestIds = @('TEST-UIMENUBAR-001') },
    [pscustomobject]@{ Pattern = '^FR-UIPERIPHERAL-001$'; TrIds = @(); TestIds = @('TEST-UIPERIPHERAL-001') },
    [pscustomobject]@{ Pattern = '^FR-ROMM-AVUI-'; TrIds = @('TR-MVVM-001'); TestIds = @('TEST-ROMM-AVUI-001') },
    [pscustomobject]@{ Pattern = '^FR-ROMM-DETAIL-'; TrIds = @('TR-ROMM-BOUNDARY-001'); TestIds = @('TEST-ROMM-DETAIL-001') },
    [pscustomobject]@{ Pattern = '^FR-ROMM-PKG-'; TrIds = @('TR-ROMM-NUGET-001'); TestIds = @('TEST-ROMM-PKG-001') },
    [pscustomobject]@{ Pattern = '^FR-VIC20-(003|004)$'; TrIds = @('TR-CYCLE-001'); TestIds = @('TEST-VIC20-001') },
    [pscustomobject]@{ Pattern = '^FR-VIC20-006$'; TrIds = @('TR-DRV-EDGE-001'); TestIds = @('TEST-VIC20-001') },
    [pscustomobject]@{ Pattern = '^FR-D71-001$'; TrIds = @(); TestIds = @('TEST-DRV1571-LOCKSTEP-001') },
    [pscustomobject]@{ Pattern = '^FR-D81-001$'; TrIds = @(); TestIds = @('TEST-DRV1581-LOCKSTEP-001') },
    [pscustomobject]@{ Pattern = '^FR-DRV1540-001$'; TrIds = @(); TestIds = @('TEST-DRV1540-LOCKSTEP-001') },
    [pscustomobject]@{ Pattern = '^FR-DRV1541II-001$'; TrIds = @(); TestIds = @('TEST-DRV1541II-LOCKSTEP-001') },
    [pscustomobject]@{ Pattern = '^FR-DRVMODEL-(001|002)$'; TrIds = @(); TestIds = @('TEST-DRV-TYPE-001') }
)
foreach ($record in $functionalLedger | Where-Object { -not $_.IsLegacy }) {
    foreach ($rule in $domainFallbackRules | Where-Object { $record.Id -match $_.Pattern }) {
        $added = $false
        foreach ($trId in $rule.TrIds) {
            if ($trId -in $knownTrIds -and -not $mappingByFr[$record.Id].TrIds.Contains($trId)) {
                [void]$mappingByFr[$record.Id].TrIds.Add($trId)
                $added = $true
            }
        }
        foreach ($testId in $rule.TestIds) {
            if ($testId -in $knownTestIds -and -not $mappingByFr[$record.Id].TestIds.Contains($testId)) {
                [void]$mappingByFr[$record.Id].TestIds.Add($testId)
                $added = $true
            }
        }
        if ($added) {
            [void]$mappingByFr[$record.Id].Evidence.Add([pscustomobject][ordered]@{
                Source = 'domain-mapping-reconstruction'
                Detail = "Recovered from FR domain and surviving TR/TEST contracts using rule $($rule.Pattern); owner approval required."
            })
        }
    }
}

foreach ($frId in @($mappingByFr.Keys)) {
    $proposedMapping = $mappingByFr[$frId]
    $proposedMapping.TrIds = @($proposedMapping.TrIds | Sort-Object -Unique)
    $proposedMapping.TestIds = @($proposedMapping.TestIds | Sort-Object -Unique)
    $beforeMapping = if ($liveMappingByFr.ContainsKey($frId)) { $liveMappingByFr[$frId] } else { $null }
    $beforeTr = if ($null -ne $beforeMapping) { @($beforeMapping.TrIds | Sort-Object -Unique) } else { @() }
    $beforeTest = if ($null -ne $beforeMapping) { @($beforeMapping.TestIds | Sort-Object -Unique) } else { @() }
    $mappingChanged = (($beforeTr | ConvertTo-Json -Compress) -cne ($proposedMapping.TrIds | ConvertTo-Json -Compress)) -or (($beforeTest | ConvertTo-Json -Compress) -cne ($proposedMapping.TestIds | ConvertTo-Json -Compress))
    if ($mappingChanged -and ($proposedMapping.TrIds.Count -gt 0 -or $proposedMapping.TestIds.Count -gt 0)) {
        $mappingOperation = if ($null -eq $beforeMapping) { 'create' } else { 'update' }
        [void]$operations.Add([pscustomobject][ordered]@{
            Order = $operations.Count + 1
            Operation = $mappingOperation
            Kind = 'mapping'
            Id = $frId
            Before = $beforeMapping
            After = [pscustomobject][ordered]@{ FrId = $frId; TrIds = $proposedMapping.TrIds; TestIds = $proposedMapping.TestIds }
            Evidence = @($proposedMapping.Evidence)
        })
        $inverse = if ($mappingOperation -eq 'create') {
            [pscustomobject][ordered]@{ Operation = 'delete'; Kind = 'mapping'; Id = $frId; Restore = $null }
        } else {
            [pscustomobject][ordered]@{ Operation = 'update'; Kind = 'mapping'; Id = $frId; Restore = $beforeMapping }
        }
        [void]$rollback.Add([pscustomobject][ordered]@{ ReverseOrder = 0; ForwardOrder = $operations.Count; Inverse = $inverse })
    }
}

foreach ($record in $functionalLedger) {
    $mapping = if ($mappingByFr.ContainsKey($record.Id)) { $mappingByFr[$record.Id] } else { $null }
    $hasTestMapping = $null -ne $mapping -and @($mapping.TestIds).Count -gt 0
    $hasTrMapping = $null -ne $mapping -and @($mapping.TrIds).Count -gt 0
    if (-not $record.IsLegacy -and (-not $hasTestMapping -or -not $hasTrMapping)) {
        [void]$invariants.Add([pscustomobject][ordered]@{
            Severity = 'blocker'
            Rule = 'Every active FR must map to at least one appropriate TR and TEST requirement.'
            Id = $record.Id
            Detail = "Recovered mapping has TR count $(@($mapping.TrIds).Count) and TEST count $(@($mapping.TestIds).Count); mapping recovery is incomplete."
        })
    }
    foreach ($trId in @($mapping.TrIds)) {
        if ($trId -notin $knownTrIds) {
            [void]$invariants.Add([pscustomobject][ordered]@{ Severity = 'blocker'; Rule = 'Every mapping reference must resolve at the same type.'; Id = $record.Id; Detail = "Missing TR reference $trId." })
        }
    }
    foreach ($testId in @($mapping.TestIds)) {
        if ($testId -notin $knownTestIds) {
            [void]$invariants.Add([pscustomobject][ordered]@{ Severity = 'blocker'; Rule = 'Every mapping reference must resolve at the same type.'; Id = $record.Id; Detail = "Missing TEST reference $testId." })
        }
    }
    if (@($record.Proposed.AcceptanceCriteria).Count -eq 0) {
        [void]$invariants.Add([pscustomobject][ordered]@{
            Severity = 'blocker'
            Rule = 'Every FR, including legacy FRs, must have validation-appropriate AC.'
            Id = $record.Id
            Detail = 'The proposed FR has zero acceptance criteria.'
        })
    }
}

$wrongType = @($ledger | Group-Object Id | Where-Object { @($_.Group.Kind | Sort-Object -Unique).Count -gt 1 })
foreach ($group in $wrongType) {
    $intendedKind = if ($group.Name -like 'FR-*') { 'fr' } elseif ($group.Name -like 'TR-*') { 'tr' } elseif ($group.Name -like 'TEST-*') { 'test' } else { '' }
    $intended = @($group.Group | Where-Object Kind -eq $intendedKind)
    $wrongLive = @($group.Group | Where-Object { $_.Kind -ne $intendedKind -and $_.LivePresent })
    if ($intended.Count -eq 1 -and $wrongLive.Count -gt 0) {
        foreach ($wrongRecord in $wrongLive) {
            [void]$operations.Add([pscustomobject][ordered]@{
                Order = $operations.Count + 1
                Operation = 'delete'
                Kind = $wrongRecord.Kind
                Id = $wrongRecord.Id
                Before = $wrongRecord.Live
                After = $null
                Evidence = [pscustomobject][ordered]@{
                    SourceClass = 'typed-identity-repair'
                    Reason = "The $($group.Name) prefix and canonical $intendedKind record establish the intended type; remove only the wrong-type placeholder after owner approval."
                }
            })
            [void]$rollback.Add([pscustomobject][ordered]@{
                ReverseOrder = 0
                ForwardOrder = $operations.Count
                Inverse = [pscustomobject][ordered]@{ Operation = 'create'; Kind = $wrongRecord.Kind; Id = $wrongRecord.Id; Restore = $wrongRecord.Live }
            })
        }
    } else {
        [void]$invariants.Add([pscustomobject][ordered]@{
            Severity = 'blocker'
            Rule = 'A requirement id must have one canonical type unless an explicitly approved compatibility exception exists.'
            Id = $group.Name
            Detail = 'Typed identities present: ' + (($group.Group.Kind | Sort-Object -Unique) -join ', ')
        })
    }
}

[void]$invariants.Add([pscustomobject][ordered]@{
    Severity = 'blocker'
    Rule = 'The approved full-set restoration must be atomic and prove complete rollback before any MCP mutation.'
    Id = 'MCP-REQUIREMENTS-ATOMICITY'
    Detail = 'Active helpers provide atomic createBatch and updateBatch only for FR/TR/TEST records. Mapping upserts, mapping deletes, and typed requirement deletes each commit independently; no supported helper wraps the full create/update/delete/mapping set in one transaction. Evidence: F:/GitHub/McpServer/src/McpServer.Support.Mcp/Controllers/RequirementsController.cs:590, F:/GitHub/McpServer/src/McpServer.Support.Mcp/Controllers/RequirementsController.cs:748, F:/GitHub/McpServer/src/McpServer.Services/Requirements/RequirementsDatabaseDocumentService.cs:752, F:/GitHub/McpServer/src/McpServer.Services/Requirements/RequirementsDatabaseDocumentService.cs:920.'
})

for ($index = 0; $index -lt $rollback.Count; $index++) {
    $rollback[$index].ReverseOrder = $rollback.Count - $index
}

$summary = [pscustomobject][ordered]@{
    GeneratedUtc = [DateTime]::UtcNow.ToString('o')
    WorkspacePath = $workspaceFull
    ReceiptDirectory = $receiptFull
    SourcePrecedence = @(
        'Latest operator direction: every FR remains a requirement, including legacy UWP/Xbox FRs; none are retired by this recovery.',
        'Current canonical docs/requirements fields.',
        'Historical Git snapshots d28493a then d1f7175.',
        'Surviving MCP record fields and mappings.',
        'Inline acceptance evidence from requirement-bearing plans/audits.',
        'Clearly labeled inference from surviving FR body only when no stronger AC source survives.'
    )
    Counts = [pscustomobject][ordered]@{
        LiveFunctional = @($livePayload.functional).Count
        LiveTechnical = @($livePayload.technical).Count
        LiveTesting = @($livePayload.testing).Count
        LiveMappings = @($livePayload.mapping).Count
        CurrentCanonical = $currentRecords.Count
        HistoricalByRef = [pscustomobject][ordered]@{}
        Ledger = $ledger.Count
        ProposedRequirementCreates = @($operations | Where-Object { $_.Operation -eq 'create' -and $_.Kind -ne 'mapping' }).Count
        ProposedRequirementUpdates = @($operations | Where-Object { $_.Operation -eq 'update' -and $_.Kind -ne 'mapping' }).Count
        ProposedExpectedWrongTypeDeletes = @($operations | Where-Object Operation -eq 'delete').Count
        ProposedMappingCreates = @($operations | Where-Object { $_.Operation -eq 'create' -and $_.Kind -eq 'mapping' }).Count
        ProposedMappingUpdates = @($operations | Where-Object { $_.Operation -eq 'update' -and $_.Kind -eq 'mapping' }).Count
        Preserved = @($ledger | Where-Object Classification -eq 'preserve').Count
        Functional = $functionalLedger.Count
        FunctionalWithZeroProposedAc = @($functionalLedger | Where-Object { @($_.Proposed.AcceptanceCriteria).Count -eq 0 }).Count
        FunctionalWithInferredAc = @($functionalLedger | Where-Object AcInferred).Count
        ActiveFunctionalWithIncompleteMapping = @($invariants | Where-Object { $_.Rule -like 'Every active FR must map*' }).Count
        Blockers = @($invariants | Where-Object Severity -eq 'blocker').Count
    }
    Guardrails = [pscustomobject][ordered]@{
        RequiresOwnerApprovalBeforeMutation = $true
        IncludesRequirementMutation = $false
        DeletesRequirements = @($operations | Where-Object Operation -eq 'delete').Count -gt 0
        DeletesOnlyExpectedWrongTypePlaceholders = $true
        RetiresLegacyRequirements = $false
        ChangesLegacyScope = $false
        DefaultStatusForRecoveredRecords = 'pending'
    }
}
foreach ($ref in $HistoricalRefs) {
    $summary.Counts.HistoricalByRef | Add-Member -NotePropertyName $ref -NotePropertyValue @($historicalByRef[$ref]).Count
}

$preview = [pscustomobject][ordered]@{
    Summary = $summary
    Invariants = @($invariants)
    ExistingMappings = @($liveMappingByFr.Values | Sort-Object FrId)
    ProposedMappings = @($mappingByFr.Values | Sort-Object FrId | ForEach-Object { [pscustomobject][ordered]@{ FrId = $_.FrId; TrIds = @($_.TrIds); TestIds = @($_.TestIds); Evidence = @($_.Evidence) } })
    Operations = @($operations)
}
$rollbackManifest = [pscustomobject][ordered]@{
    GeneratedUtc = $summary.GeneratedUtc
    WorkspacePath = $workspaceFull
    Precondition = 'Apply only to the immutable live snapshot captured in this receipt directory. Re-query and hash live state immediately before mutation.'
    FullRollbackProven = $false
    Reason = 'Inverse operations are generated, but the active supported helper surface has no single atomic transaction for requirement creates, requirement updates, wrong-type deletes, and mapping upserts. Mapping and delete calls commit independently. Mutation is blocked pending a separate MCP Server capability change.'
    Operations = @($rollback | Sort-Object ReverseOrder)
}

$jsonOptions = @{ Depth = 100 }
[IO.File]::WriteAllText((Join-Path $receiptFull 'recovery-ledger.json'), ($ledger | ConvertTo-Json @jsonOptions), [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $receiptFull 'staged-recovery-preview.json'), ($preview | ConvertTo-Json @jsonOptions), [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $receiptFull 'rollback-manifest.json'), ($rollbackManifest | ConvertTo-Json @jsonOptions), [Text.UTF8Encoding]::new($false))

$csvRows = foreach ($record in $ledger) {
    [pscustomobject][ordered]@{
        Kind = $record.Kind
        Id = $record.Id
        Legacy = $record.IsLegacy
        Classification = $record.Classification
        CurrentCanonical = $record.CurrentCanonicalPresent
        Historical = $record.HistoricalPresent
        Live = $record.LivePresent
        AcCount = @($record.Proposed.AcceptanceCriteria).Count
        AcSource = $record.AcSourceClass
        AcInferred = $record.AcInferred
        Issues = ($record.Issues -join ' | ')
    }
}
$csvRows | Export-Csv -LiteralPath (Join-Path $receiptFull 'recovery-ledger.csv') -NoTypeInformation -Encoding utf8NoBOM

$markdown = [System.Collections.Generic.List[string]]::new()
[void]$markdown.Add('# Requirements Recovery Staged Preview')
[void]$markdown.Add('')
[void]$markdown.Add("Generated UTC: $($summary.GeneratedUtc)")
[void]$markdown.Add('')
[void]$markdown.Add('No MCP requirement mutation has been performed. Owner approval is required after all blockers are resolved.')
[void]$markdown.Add('')
[void]$markdown.Add('## Guardrails')
[void]$markdown.Add('')
[void]$markdown.Add('- Every FR remains a requirement, including legacy UWP/Xbox FRs.')
[void]$markdown.Add('- No legacy requirement is retired, deleted, or scoped inactive.')
[void]$markdown.Add('- Every FR must have validation-appropriate acceptance criteria and at least one mapped TEST requirement.')
[void]$markdown.Add('- Restored but unproved behavior remains pending.')
[void]$markdown.Add('- Mutation is blocked until atomic rollback is proven and the owner approves this exact preview.')
[void]$markdown.Add('')
[void]$markdown.Add('## Counts')
[void]$markdown.Add('')
foreach ($property in $summary.Counts.PSObject.Properties | Where-Object Name -ne 'HistoricalByRef') {
    [void]$markdown.Add("- $($property.Name): $($property.Value)")
}
foreach ($property in $summary.Counts.HistoricalByRef.PSObject.Properties) {
    [void]$markdown.Add("- Historical $($property.Name): $($property.Value)")
}
[void]$markdown.Add('')
[void]$markdown.Add('## Blocking invariants')
[void]$markdown.Add('')
if ($invariants.Count -eq 0) {
    [void]$markdown.Add('- None.')
} else {
    foreach ($item in $invariants | Sort-Object Rule, Id) {
        [void]$markdown.Add("- [$($item.Severity)] $($item.Id): $($item.Detail)")
    }
}
[void]$markdown.Add('')
[void]$markdown.Add('## Operation index')
[void]$markdown.Add('')
foreach ($operation in $operations) {
    $acMode = if ($operation.Operation -eq 'delete') {
        'EXPECTED WRONG-TYPE PLACEHOLDER REMOVAL - OWNER APPROVAL REQUIRED'
    } elseif ($operation.Kind -eq 'mapping') {
        "mapping evidence entries $(@($operation.Evidence).Count)"
    } elseif ($operation.Evidence.InferredAcceptanceCriteria) {
        'AC INFERRED - OWNER REVIEW REQUIRED'
    } else {
        $operation.Evidence.AcceptanceCriteriaSourceClass
    }
    [void]$markdown.Add("- $($operation.Order). $($operation.Operation.ToUpperInvariant()) $($operation.Kind.ToUpperInvariant()) $($operation.Id): $acMode")
}
[IO.File]::WriteAllLines((Join-Path $receiptFull 'staged-recovery-preview.md'), $markdown, [Text.UTF8Encoding]::new($false))

$hashes = Get-ChildItem -LiteralPath $receiptFull -File | Where-Object Name -ne 'artifact-hashes.json' | Sort-Object Name | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    [pscustomobject][ordered]@{ Name = $_.Name; Length = $_.Length; Sha256 = $hash.Hash }
}
[IO.File]::WriteAllText((Join-Path $receiptFull 'artifact-hashes.json'), ($hashes | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))

$summary | ConvertTo-Json -Depth 20
