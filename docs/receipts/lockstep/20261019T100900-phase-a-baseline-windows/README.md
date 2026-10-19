# Phase A step 1 part 1: Windows native-safe baseline, PAYTON-LEGION2

Receipt for Phase A step 1 part 1 of `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` (section 4, Phase A, item 1): the three native-safe test projects run unfiltered in Release on PAYTON-LEGION2, with the `native/vice` inventory captured before and after. No product code, no requirement, TODO or plan state was changed by this run, and no hostile validation applies (operational class, remediation phase, Payton 2026-10-19).

This README was written 2026-10-19 on PAYTON-LEGION2 by Claude Code (claude-opus-5-5) after the run, from the files in this folder. The run itself was executed by the `lab-wsman` account (file owner `PAYTON-LEGION2\lab-wsman`, `summary.json` field `user`), driven from LAB-OMARCHY by the vice-sharp-b1 session over the WSMan route. The script that produced the files is not stored in this folder.

## When, where, what was tested

- Machine: PAYTON-LEGION2 (`summary.json` field `machine`).
- Run window: 2026-10-19T10:05:31 to 2026-10-19T10:05:53 local (`run.log`). Observation: the folder stamp `20261019T100900` is later than the measured run start; the stamp is a label, not the start time.
- dotnet: `C:\Program Files\dotnet\dotnet.exe`, SDK `10.0.402`.
- Repository: branch `develop`, HEAD `d00e8c5584d0031b3637c649335964f8d5225da0` at run time. `native/vice` gitlink `cc67418cba58e71d8e70fd353e9de47199ad2075`.
- Command shape, per project: `dotnet test <project>.csproj -c Release --nologo --logger trx;LogFileName=<Name>.trx --results-directory <this folder>`. No filter. `native/Vice.Native.proj` is not referenced by these three projects and was not invoked.

## Results

- `tests/ViceSharp.OracleContract.Tests`: exit 0, total 89, passed 89, failed 0, skipped 0 (`OracleContract.trx`, `OracleContract.console.log`).
- `tests/ViceSharp.VsfCompatibility.Tests`: exit 0, total 199, passed 199, failed 0, skipped 0 (`VsfCompatibility.trx`, `VsfCompatibility.console.log`).
- `tests/ViceSharp.NativeClockProbe.Tests`: exit 0, total 69, passed 69, failed 0, skipped 0 (`NativeClockProbe.trx`, `NativeClockProbe.console.log`).
- Combined: 357 executed, 357 passed, 0 failed, 0 skipped.

Scope limit: these three projects are not in `ViceSharp.slnx` (handoff v2 section 2.3). This receipt is not a full-solution result and does not cover the TestHarness diagnostic filters (Phase A step 1 part 2).

## Native inventory

- Before: 2,539 entries (12 tracked changes, 2,527 untracked), SHA256 `C7848A24867CD60C49FE6088692A6E4565240868C89D811F806B429CDB3CBFC5`.
- After: 2,539 entries, same SHA256.
- `nativeInventoryUnchanged: true` (`summary.json`). The run did not touch `native/vice`.

## Rerun

`../20261019T101000-phase-a-baseline-windows/` holds a second run of the same commands (2026-10-19T10:06:40 to 10:06:52) with identical counts (89/0/0, 199/0/0, 69/0/0) and the identical inventory hash. Its TRX and log bytes differ only because they carry their own timestamps and paths. This folder is the receipt of record; the rerun is retained, not deleted.

## Files and SHA256

- `native-inventory-after.txt` (102,834 bytes): `C7848A24867CD60C49FE6088692A6E4565240868C89D811F806B429CDB3CBFC5`
- `native-inventory-before.txt` (102,834 bytes): `C7848A24867CD60C49FE6088692A6E4565240868C89D811F806B429CDB3CBFC5`
- `NativeClockProbe.console.log` (792 bytes): `099EBE72CE84A0837438B666A6DCEB6D86C69F56DD6F326300B5FF20C0216131`
- `NativeClockProbe.trx` (107,852 bytes): `5782E4696C9E7A01E516A6A5C66664A5CEF4D9D3F03A2065F4AA5A962E5E1AB2`
- `OracleContract.console.log` (779 bytes): `08594FAF1E27F206A39FAC0E5638C6E625418E6C666983C5512D7C0B8C8A32F6`
- `OracleContract.trx` (140,660 bytes): `29930FCC75F2FCB71EC447DC490D2790E293B2EA8553B682678E71ECA30B2EA0`
- `run.log` (1,924 bytes): `C2B582B3F04A860531DF83A93448B323D1220235E86A0667D304C82B57C22154`
- `summary.json` (704 bytes): `DD03D78BD77FF177076FEFB8889B36D82B17BA0952E6A683CC88B448863900AB`
- `VsfCompatibility.console.log` (1,044 bytes): `F5F922B94CF6C3EB7DAE25F20126C16AADBD1BB4BA6573F40D867B65C68B646C`
- `VsfCompatibility.trx` (313,584 bytes): `2DE9D533825099E21CB173F9021BA7921042AC7A08054BB3D80ED9D6586B3946`

## Ratings

Accuracy 97: every count, time, hash and path above is read from the files in this folder or measured on disk on 2026-10-19; the one inference (the run was driven from LAB-OMARCHY over WSMan) rests on the `lab-wsman` owner and the vice-sharp-b1 brief, not on a log line. Completeness 92: the driving script and its analyzer result are not in this folder, and the Linux receipt this README was meant to mirror (handoff section 10) was not available on PAYTON-LEGION2 when it was written.
