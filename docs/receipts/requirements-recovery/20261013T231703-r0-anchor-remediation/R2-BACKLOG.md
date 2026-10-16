# Mandatory R2 fixture-design backlog

Owner: the implementing agent under PLAN-REQAUDIT-001 and the original immutable-oracle plan. Entry: approved/read-back R1 requirements and accepted application gate. Exit: all 117 distinct groups in candidate coverage have complete independent fixtures, exact expected observations, assertions and discriminating negative controls; the full applicable mock suite passes with zero failures, skips or aborted/unexecuted required cases, followed by required HV. No product implementation begins before that exit.

The 96/93 reviewer called these "32" names, including "the ten Turbo_* groups". The actual list is 21 other names plus **11** Turbo_* names, totaling32. All32 are listed below. They currently have AC text and named bindings, not complete standalone case matrices or executable test evidence. Their requirement/AC bindings are the exact `namedCaseGroups` rows in the candidate coverage, not a second mapping system.

For each group, R2 must record requirement/AC IDs, legal input/asset manifests, initial process/reset/VSF history, source-derived expected values and event order, provider seam and observation capability, positive/boundary/error cases, and an independently corrupt response for every relevant assertion. A correct mock must exercise the real assertion; negative controls must reject inside a green harness. The same fixtures and assertions later bind to real providers. A missing external native observation blocks real acceptance; it never authorizes editing VICE.

## All32 AC-text-only groups

1. Tap_BlockDecoderVersusDatasettePath
2. Tap_ClockHeaderVersusSelectedMachine
3. Tap_HeaderWarningVersusRejection
4. TapV0_ZeroDoesNotConsumeExtension
5. TapV1V2_ExtensionAndTruncation
6. Tap_CbmThresholdBoundaries
7. Tap_TurboPilotHeaderBoundaries
8. SenseDongle_EnableDisablePowerup
9. CpClockF83_MotorWriteSenseTruthTable
10. TapeContract_RejectsMissingNativeEvidence
11. TapeContract_UnchangedAssertionsAcrossProviders
12. TapeContract_FullCumulativeGate
13. TapePublicApi_MotorState
14. TapePublicApi_MountEject
15. DatasetteAudio_ResourceBufferAndSamples
16. TapeRead_TimerInterruptTrace
17. TapeRead_CbmHeaderDataChecksum
18. TapeRead_NormalSpeedMachineTrace
19. TapeWrite_KernalSaveAndReload
20. TapeWrite_NewExistingAndAppendPosition
21. TapeRead_TurboNamedLoaderMatrix
22. Turbo_Biturbo_LoadSuccess
23. Turbo_C64Flag_ExactClockAndPendingState
24. Turbo_CustomEncoding_OrderedObservations
25. Turbo_Cyberload_LoadSuccess
26. Turbo_HalfFullWave_BlockVsLive
27. Turbo_MidLoadRateTransition_Continuity
28. Turbo_Novaload_LoadSuccess
29. Turbo_Pavloda_LoadSuccess
30. Turbo_PulseWidths_Around160_NoArtificialFloor
31. Turbo_Turbotape_LoadSuccess
32. Turbo_Vic20Ca1_RemainsSeparate

## Required sequencing, not waiver

- Loader groups require lawful named workload/firmware/resource manifests and successful native observations, not generic tape-source citations or agreement between two failed loads. Exact loader-specific evidence is presently absent.
- Timer/interrupt groups must observe the actual machine-specific CIA FLAG versus VIA2 CA1 path and ordered pending/acknowledgement state, not cassette-sense lines.
- Tests that depend on a complete observer or fault-injection capability first exercise independent mock seams; real binding stays blocked until that capability is proven against immutable native VICE.
- The other85 groups already have additional specification material, not a passed mock gate. They remain in the same complete R2 denominator and must meet the identical exit.
- Eight unchanged baseline AC and each existing broad CPU/profile condition remain mandatory in their applicable whole-plan gates. This backlog does not redefine the whole requirement universe as117 groups.
- D64 missing TR/TEST, extended-track scope and true-drive requirements remain separate approval dependencies. No D64/drive implementation or complete-media claim until those decisions and gates are satisfied.

R0 reviews the requirement text and this explicit stage allocation. R0 does not claim these32 designs or any executable gate are finished. A later agent may not interpret R0 AGREE as R2 AGREE.
