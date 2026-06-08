# ViceSharp.Benchmarks

BenchmarkDotNet harness for the ViceSharp managed emulator core. Tracks MCP
TODO `PERF-BENCHMARK-001`.

## Scope (current slice)

Managed-only measurements. The harness exercises each subsystem in isolation
and the full machine end-to-end:

- `CpuBenchmarks` - tick the MOS 6502/6510 through a NOP-filled RAM
- `VicIiBenchmarks` - tick the MOS 6569 for one PAL frame
- `SidBenchmarks` - tick the MOS 6581 with a sawtooth voice + ADSR
- `CiaBenchmarks` - tick the MOS 6526 with a short Timer A latch
- `PubSubBenchmarks` - measure TR-PUBSUB-001 publish, delivery, pool, and arena costs
- `FullSystemBenchmark` - drive the romless Commodore64 via `IClock.Step`

These benchmarks do not require ROMs or a native VICE build; everything runs
offline against in-memory RAM.

## Running

```bash
dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --filter "*"
```

To run a single class:

```bash
dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --filter "*CpuBenchmarks*"
```

For the quick TR-PUBSUB-001 stopwatch probe:

```bash
dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --pubsub-probe 1000000
```

BenchmarkDotNet writes reports under `BenchmarkDotNet.Artifacts/results/`.

## CI integration

The benchmark binary is not invoked from `dotnet test`. The smoke test
`BenchmarksSmokeTests` in `tests/ViceSharp.TestHarness` runs one iteration of
each benchmark setup + workload so the wiring stays compilable and the
workloads keep matching the live chip surface area.

## TODO(perf-vs-vice)

`NativeViceBaseline.cs` is a placeholder for the upcoming native VICE
comparison slice. It will invoke the shim built from `native/vice/...` via
`ViceSharp.Core.ViceNative`, run the same scripted workloads, and emit
measurements in a shared format so the Completion Dashboard can compare
managed and native cycles per second. Tracked as remaining work under
`PERF-BENCHMARK-001`.
