# Receipt: Vic20 RAM preset All stack overflow (2026-08-06)

## Symptom
Xbox Settings: selecting VIC-20 RAM preset **All** crashed the app.

## Evidence (WER)
- Time: 2026-08-06 23:53:14
- Faulting app: ViceSharp.Xbox.exe 1.2.7.0
- Faulting module: Windows.UI.Xaml.dll
- Exception: **0xc00000fd** (STATUS_STACK_OVERFLOW)
- settings.json still Vic20MemorySpec=none (crash on selection, before Apply)

## Root cause
TwoWay ComboBox/ToggleSwitch write-back re-entered Vic20 memory setters. Setters always raised PropertyChanged for SelectedVic20MemoryPreset + all BLK toggles even when the map was unchanged, producing an unbounded re-notify loop in XAML.

## Fix
- Vic20MemoryUiState: SetFromSpec/SetFromPresetLabel/TrySetBit return whether the map changed
- XboxSettingsViewModel + AttachPanelViewModel: only Notify when the map actually changed

## Test
dotnet test tests/ViceSharp.TestHarness -c Release --filter FullyQualifiedName~SelectedVic20MemoryPreset_All_SurvivesTwoWayBindingReentrancy|Vic20MemorySettingsRestart|Vic20SettingsUiSurface|Vic20RamBlocks
Result: Passed 69 / Failed 0 / Skipped 0

## Second crash (2026-08-07 00:00:50) after first fix
- Still STATUS_STACK_OVERFLOW / System.StackOverflowException (WinRT.Runtime)
- Not a graphics driver fault; WER P9 = System.StackOverflowException
- settings.json had Vic20MemorySpec=all (Apply persisted successfully before UI died)
- BlueScreen WER earlier was a false-positive style event; user reports no real BSOD

## Hardening
- Ignore null/empty ComboBox TwoWay clears (prevent All <-> Unexpanded oscillation)
- _vic20MemoryNotifyBusy latch during PropertyChanged push
- Block setters no-op while notifying
- Stop notifying IsVic20Selected on memory-only changes
- Adopt/Restore use same safe NotifyVic20Memory path

## Tests
XboxSettingsViewModelTests including null-clear simulation: 6 passed
