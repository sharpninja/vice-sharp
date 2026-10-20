[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
if (-not $IsLinux) { throw 'The permitted managed diagnostic is Linux-only.' }
$projectPath = '/home/sharpninja/github/vice-sharp/tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj'
$jitPath = Join-Path $PSScriptRoot 'discovery-jit.log'
$testArgs = @('test',$projectPath,'-c','Release','--no-build','--no-restore','--filter','FullyQualifiedName~Vic20HalfCycleComparatorTests','--results-directory',(Join-Path $PSScriptRoot 'TestResults'),'--logger','trx;LogFileName=vic20-comparator-discovery.trx')
if (-not $PSCmdlet.ShouldProcess(($testArgs -join ' '), 'Repeat the permitted comparator filter with a scoped JIT diagnostic')) { return }
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = (Get-Command dotnet).Source
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
foreach ($argument in $testArgs) { $startInfo.ArgumentList.Add($argument) }
$startInfo.Environment['DOTNET_JitDisasm'] = 'ViceSharp.Core.ViceNative:FindLibraryPath'
$startInfo.Environment['DOTNET_JitStdOutFile'] = $jitPath
$startInfo.Environment['DOTNET_ReadyToRun'] = '0'
$process = [System.Diagnostics.Process]::Start($startInfo)
$stdoutTask = $process.StandardOutput.ReadToEndAsync()
$stderrTask = $process.StandardError.ReadToEndAsync()
$process.WaitForExit()
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'discovery-test.stdout.log'),$stdoutTask.GetAwaiter().GetResult())
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'discovery-test.stderr.log'),$stderrTask.GetAwaiter().GetResult())
$result = [ordered]@{ timestampLocal = (Get-Date).ToString('s'); command = 'dotnet'; arguments = $testArgs; exitCode = $process.ExitCode; jitFileExists = Test-Path -LiteralPath $jitPath; diagnosticEnvironment = @{ DOTNET_JitDisasm = 'ViceSharp.Core.ViceNative:FindLibraryPath'; DOTNET_JitStdOutFile = $jitPath; DOTNET_ReadyToRun = '0' } }
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'discovery-test-result.json') -Encoding utf8
$result | ConvertTo-Json -Depth 8
if (Test-Path -LiteralPath $jitPath) { Select-String -LiteralPath $jitPath -Pattern 'Assembly listing|FindLibraryPath' }
