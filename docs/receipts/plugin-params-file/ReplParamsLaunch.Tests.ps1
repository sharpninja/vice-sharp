#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }

# Params never cross a process boundary on the command line (triage-report-821fe2da58284b4ea21604cb2da2fccb).
# A Windows child command line is limited to 32,767 characters, so every launcher of lib/repl-invoke.ps1 in a new
# process hands params over with lib/repl-params-transfer.ps1 (-ParamsPath file), never as -ParamsYaml text.
# The core files are covered by McpServer plugins/core/test-fixtures/pester/ReplParamsTransfer.Tests.ps1; this suite
# covers the whole plugin repo, including host-specific launchers (hooks/scripts, entry scripts).
# BDPv4: Tag MockGate proves the lint finds each launch pattern and ignores in-process calls; Tag Binding runs it
# over this repo.

BeforeAll {
    $script:PluginRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

    function Get-ParamsCommandLineLaunch {
        param([string[]]$Lines)
        $hits = [System.Collections.Generic.List[string]]::new()
        for ($i = 0; $i -lt $Lines.Count; $i++) {
            $l = $Lines[$i]
            if ($l -notmatch '-ParamsYaml') { continue }
            if ($l -match 'ArgumentList\.Add|-Arguments\s+@\(|foreach\s*\(\s*\$\w+\s+in\s+@\(.*''-File''|Start-Process') { $hits.Add("$($i + 1): $($l.Trim())") }
        }
        return $hits.ToArray()
    }

    function Get-RepoScriptFile {
        Get-ChildItem -LiteralPath $script:PluginRoot -Recurse -File -Include '*.ps1', '*.psm1' |
            Where-Object { $_.FullName -notmatch '[\\/](tests|\.git|node_modules|\.worktrees)[\\/]' }
    }
}

Describe 'Launch lint discriminates (mock gate)' -Tag MockGate {
    It 'finds <Name>' -ForEach @(
        @{ Name = 'wrapper -Arguments'; Line = "Invoke-PluginPowerShellScript -ScriptPath `$x -Arguments @('-Method', `$m, '-ParamsYaml', (`$t ?? ''))" }
        @{ Name = 'entry -Arguments'; Line = "Invoke-PowerShellPluginScript -ScriptPath (Join-Path `$r 'lib\repl-invoke.ps1') -Arguments @('-Method', `$Method, '-ParamsYaml', (`$p ?? ''))" }
        @{ Name = 'ArgumentList loop'; Line = "foreach (`$argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', `$repl, '-Method', `$Method, '-ParamsYaml', `$ParamsYaml)) {" }
        @{ Name = 'ArgumentList.Add'; Line = "`$psi.ArgumentList.Add('-ParamsYaml')" }
    ) { $h = @(Get-ParamsCommandLineLaunch -Lines @('x', $Line)); $h.Count | Should -Be 1; $h[0] | Should -BeLike '2: *' }
    It 'ignores in-process calls and file transfer' {
        @(Get-ParamsCommandLineLaunch -Lines @("& `$repl -Method `$m -ParamsYaml `$y", "Invoke-ReplRaw -Method `$m -ParamsYaml `$y", "-Arguments (@('-Method', `$m) + @(`$t.Arguments))")).Count | Should -Be 0
    }
}

Describe 'Plugin launchers use the params-file transfer (binding)' -Tag Binding {
    It 'ships the core transfer helper' {
        Test-Path -LiteralPath (Join-Path $script:PluginRoot 'lib/repl-params-transfer.ps1') -PathType Leaf | Should -BeTrue
    }
    It 'lib/repl-invoke.ps1 accepts -ParamsPath' {
        $ast = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $script:PluginRoot 'lib/repl-invoke.ps1'), [ref]$null, [ref]$null)
        @($ast.ParamBlock.Parameters.Name.VariablePath.UserPath) | Should -Contain 'ParamsPath'
    }
    It 'no script in the repo launches repl-invoke.ps1 with -ParamsYaml on a command line' {
        $hits = foreach ($f in Get-RepoScriptFile) {
            foreach ($h in (Get-ParamsCommandLineLaunch -Lines ([System.IO.File]::ReadAllLines($f.FullName)))) { "$($f.FullName.Substring($script:PluginRoot.Length + 1)):$h" }
        }
        @($hits) | Should -BeNullOrEmpty
    }
    It 'every script that creates a params transfer also removes it' {
        foreach ($f in Get-RepoScriptFile) {
            $text = [System.IO.File]::ReadAllText($f.FullName)
            if ($f.Name -eq 'repl-params-transfer.ps1' -or $text -notmatch 'New-McpReplParamsTransfer') { continue }
            $text | Should -Match 'Remove-McpReplParamsTransfer' -Because $f.Name
        }
    }
}
