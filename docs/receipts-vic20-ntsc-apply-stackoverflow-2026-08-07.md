# Receipt: VIC-20 PAL->NTSC Apply stack overflow (2026-08-07)

## Symptom
Crash after selecting NTSC model and Apply. settings.json already ProfileId=vic20ntsc (host apply+persist succeeded); process died during UI rebind.

## Cause
SelectedProfileId always rebuilt Models (new list) and notified SelectedModel/SelectedComputer. WinUI ComboBox TwoWay write-back looped (same class as RAM All stack overflow).

## Fix
- Rebuild Models only when computer family changes (or Profiles catalog force-refresh)
- _modelPickerBusy latch; ignore null SelectedModel; no-op when same profile id
- Apply begin/done/error logging

## Tests
15 passed including Vic20PalToNtsc_Apply_SurvivesTwoWayModelPickerReentrancy
