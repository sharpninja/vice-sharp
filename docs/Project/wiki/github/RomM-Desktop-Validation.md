# RomM desktop validation

This is the managed Windows desktop/library gate for the non-Xbox RomM integration.
It does not replace native VICE, emulator acceptance, snapshot interoperability,
live-server checks, or the paused requirement-realignment gates.

## Commands

From the repository root in PowerShell:

```powershell
./build.ps1 RommDesktopTest --configuration Release --romm-contract-mocks
./build.ps1 RommDesktopTest --configuration Release
```

The target is implemented in [Build.Romm.cs](https://github.com/sharpninja/vice-sharp/blob/main/build/Build.Romm.cs). It builds
the selected managed test projects through `dotnet test`, retains TRX files under
`TestResults/romm-desktop/<local timestamp>-mocks` or `-real`, and fails when a
selected suite executes zero tests or has any non-passing result. It does not
invoke `ViceSharp.TestHarness`, the full solution, or a native VICE build.

## Mocks first, then production

`--romm-contract-mocks` selects `RommDesktopContractTests` in
`ViceSharp.Heads.Tests` and supplies deterministic mock observations through the
contract seam. Correct observations must pass the assertions, and deliberate
wrong observations must be rejected inside a passing negative-control harness.
Only after this gate is green may a behavior-changing implementation begin.

Without the switch, the target binds the same contract cases to production
desktop controls, ViewModels and loader boundaries, then runs all selected
Heads and Library tests. It restores the prior `VICESHARP_ROMM_CONTRACT_MOCKS`
environment value when it finishes. Current contract references include
`FR-ROMM-AVUI-001`, `FR-ROMM-COVER-001` and `TEST-ROMM-AVUI-001`; focused tests
do not establish satisfaction of every acceptance criterion in those records.

On Windows only, the real Library run excludes
`ViceSharp.Library.Tests.Packaging.UserLocalDeploymentTests`: 13 cases requiring
Linux. The operator explicitly approved this exclusion. The tests remain in the
repository and are not excluded by this target on Linux. No Linux validation is
claimed by the Windows result.

## Retained integration evidence

The [2026-10-16 integration receipt](https://github.com/sharpninja/vice-sharp/blob/main/docs/receipts/worktree-integration/20261016T102915-review.md)
and [validation summary](https://github.com/sharpninja/vice-sharp/blob/main/docs/receipts/worktree-integration/validation-summary.json)
record the exact commands, chronology and hashed logs/TRX files for product commit
`8a5ca856e63ff05b971a596d0655d647db66db70`:

- Mock gate: 46 passed, 0 failed, 0 skipped. This comprises 25 production-bound
  scenarios and 21 assertion-helper negative controls, not 46 independent UX cases.
- Production gate: 61 Heads plus 201 Library tests passed, 0 failed, 0 skipped
  within the approved Windows scope. Nineteen behavior observations are retained
  in each applicable contract TRX output.
- Native VICE: 6,374 file hashes unchanged at the recorded check; its preexisting
  local modifications were preserved, not described as a clean upstream checkout.
- Further HV was explicitly waived. Earlier DISAGREE receipts remain evidence;
  neither these tests nor this documentation constitutes an AGREE verdict.

No live RomM server session, full emulator run or whole-machine parity acceptance
is claimed by these receipts. Historical plans and legacy requirements remain
active evidence or pending work according to their own status.
