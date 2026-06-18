using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

/// <summary>
/// Host-owned emulation worker - the day-one decoupling design (docs/Decoupling.md):
/// the emulator runs on a dedicated background thread that continuously advances each
/// Running session's master clock, while the UI thread only renders the committed
/// framebuffer (<see cref="LocalVideoFrameSource"/> is a lock-free pull).
///
/// BUG-THROTTLE-001: the worker advances the CPU clock in clean whole-instruction groups
/// (so a pause/snapshot always lands on a consistent CPU boundary) and is throttled to
/// real time by a pluggable <see cref="IEmulationGate"/> strategy ("Semaphore" or "VICE").
/// This service owns the worker thread, the cycle advancement, and per-frame bookkeeping;
/// the gate owns the pacing (how much to run per tick and how to block/sleep). Video frames
/// are a side effect of the VIC reaching end-of-frame; per-frame bookkeeping hangs off
/// <see cref="IVideoChip.FrameCompleted"/>.
///
/// Registered as an <see cref="IHostedService"/>; unit tests drive <see cref="PumpSession"/>
/// directly (one deterministic clean-instruction group - no pacing wait, which lives in the gate).
/// </summary>
public sealed class EmulationPumpService : IHostedService, IDisposable
{
    // The CPU is stepped a whole instruction at a time in fixed groups so the worker
    // always stops on a clean instruction boundary (never mid-instruction).
    private const int InstructionGroupSize = 5;

    private readonly EmulatorRuntimeRegistry _registry;
    private readonly ConditionalWeakTable<EmulatorRuntimeSession, FrameBookkeeping> _frames = new();
    private readonly Func<EmulatorRuntimeSession, long, long> _advance;
    private volatile IEmulationGate _gate;
    private volatile string? _pendingStrategyId;
    private Thread? _workerThread;
    private volatile bool _running;

    public EmulationPumpService(EmulatorRuntimeRegistry registry)
        : this(registry, SelectGate())
    {
    }

    public EmulationPumpService(EmulatorRuntimeRegistry registry, IEmulationGate gate)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(gate);
        _registry = registry;
        _gate = gate;
        _advance = (session, cycles) => AdvanceCleanly(session, cycles);
    }

    /// <summary>Active pacing strategy name (e.g. "Semaphore", "VICE").</summary>
    public string GateName => _gate.Name;

    /// <summary>
    /// Switch the active pacing strategy ("semaphore" | "vice", or their display names);
    /// idempotent and safe from any thread. On a running pump the swap is staged and
    /// performed by the worker thread at the top of its next loop, so the old gate is never
    /// disposed mid-Tick; on a not-yet-started pump it takes effect immediately (StartAsync
    /// starts the new gate).
    /// </summary>
    public void SetStrategy(string? strategyName)
    {
        var id = EmulationGateStrategies.Normalize(strategyName);
        if (string.Equals(_gate.Name, EmulationGateStrategies.DisplayName(id), StringComparison.Ordinal))
            return; // already active

        if (_running)
            _pendingStrategyId = id;    // worker swaps at its next loop iteration
        else
            SwapGate(id, start: false); // not started yet: StartAsync starts the new gate
    }

    // Strategy selection at construction: VICESHARP_PACE = "vice" | "semaphore" (default).
    private static IEmulationGate SelectGate()
        => EmulationGateStrategies.CreateGate(Environment.GetEnvironmentVariable("VICESHARP_PACE"));

    // Replace the active gate. start=false for a not-yet-running pump (StartAsync will start
    // it); start=true when swapping a live gate on the worker thread.
    private void SwapGate(string strategyId, bool start)
    {
        var newGate = EmulationGateStrategies.CreateGate(strategyId);
        var old = _gate;
        if (start)
            newGate.Start();
        _gate = newGate;
        if (start)
            old.Stop();
        old.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_running)
            return Task.CompletedTask;

        _running = true;
        _gate.Start();
        _workerThread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "ViceSharp.Emulation",
            // Mostly blocks in the gate; raise priority so a busy foreground GUI cannot
            // starve it of CPU when the gate releases it.
            Priority = ThreadPriority.AboveNormal,
        };
        _workerThread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Shutdown();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Shutdown();
        _gate.Dispose();
    }

    private void Shutdown()
    {
        if (!_running && _workerThread is null)
            return;

        _running = false;
        _gate.Stop(); // unblocks the worker if it is waiting inside the gate
        _workerThread?.Join(TimeSpan.FromSeconds(2));
        _workerThread = null;
    }

    private void Loop()
    {
        while (_running)
        {
            // Live strategy switch: swap the gate on this worker thread (the only thread
            // that calls Tick) so the outgoing gate is never disposed mid-Tick.
            var pending = _pendingStrategyId;
            if (pending is not null)
            {
                _pendingStrategyId = null;
                SwapGate(pending, start: true);
            }

            // The gate advances + paces every Running session; idle when none are running.
            if (!_gate.Tick(_registry, _advance))
                Thread.Sleep(8);
        }
    }

    /// <summary>
    /// Advance one session by a single deterministic step (one clean instruction group
    /// with the limiter on, or a warp cycle burst with it off) and run any per-frame
    /// bookkeeping. Test entry point - does NOT pace (pacing lives in the gate).
    /// Returns the master cycles advanced.
    /// </summary>
    public long PumpSession(EmulatorRuntimeSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.LimiterEnabled)
            return AdvanceCleanly(session, 1);

        long warpTarget;
        lock (session.SyncRoot)
            warpTarget = Math.Max(1, (long)(session.Machine.Clock.FrequencyHz / 1000.0)) * 64;
        return AdvanceCleanly(session, warpTarget);
    }

    /// <summary>
    /// Advance one session by whole instructions until at least <paramref name="targetCycles"/>
    /// master cycles have elapsed, always ending on a clean instruction boundary, then run
    /// any per-frame bookkeeping that came due. Returns the cycles advanced.
    /// </summary>
    private long AdvanceCleanly(EmulatorRuntimeSession session, long targetCycles)
    {
        if (targetCycles <= 0)
            return 0;

        lock (session.SyncRoot)
        {
            if (session.RunState != EmulatorRunState.Running)
                return 0;

            var frame = _frames.GetValue(session, static _ => new FrameBookkeeping());
            EnsureFrameSubscription(session, frame);
            EnsureHistorySubscription(session, frame);

            var machine = session.Machine;
            var clock = machine.Clock;
            var before = clock.TotalCycles;

            do
            {
                for (var i = 0; i < InstructionGroupSize; i++)
                    machine.StepInstruction();
            }
            while (clock.TotalCycles - before < targetCycles && session.RunState == EmulatorRunState.Running);

            var advanced = clock.TotalCycles - before;

            DrainCompletedFrames(session, frame);
            session.UpdatePerformanceCounters();
            return advanced;
        }
    }

    private void EnsureFrameSubscription(EmulatorRuntimeSession session, FrameBookkeeping frame)
    {
        if (frame.Subscribed)
            return;

        frame.Subscribed = true;
        if (session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip videoChip)
            return; // headless/minimal machine: no frames, nothing to count.

        // FrameCompleted fires on this worker thread inside the step, while the framebuffer
        // holds a whole frame; commit a tear-free snapshot for the UI pull at that instant
        // and bump a worker-local counter that DrainCompletedFrames consumes after the step.
        var clock = session.Machine.Clock;
        frame.VideoChip = videoChip;
        frame.FrameHandler = (_, _) =>
        {
            session.CommitFrame(videoChip, clock.TotalCycles);
            frame.PendingFrames++;
        };
        videoChip.FrameCompleted += frame.FrameHandler;
    }

    // Subscribe the session's tick-history recorder to the machine pub/sub once: memory
    // writes (write-deltas) and instruction-completed events feed the rolling 100-tick
    // trace for the time-travel debugger. Subscribing turns on the bus/CPU publish gates.
    private static void EnsureHistorySubscription(EmulatorRuntimeSession session, FrameBookkeeping frame)
    {
        if (frame.HistorySubscribed)
            return;

        frame.HistorySubscribed = true;
        if (session.Machine.PubSub is not { } pubSub)
            return; // machine without a pub/sub (e.g. minimal headless): no history.

        var history = session.TickHistory;

        // Register the chips whose full state is captured per tick (before subscribing, so
        // the very first instruction captures chip state too). Order = device-registry order;
        // the host decodes per-tick chip state by iterating the same devices in the same order.
        var statefulDevices = session.Machine.Devices.All.OfType<IStatefulDevice>().ToArray();
        if (statefulDevices.Length > 0)
            history.SetStatefulDevices(statefulDevices);

        pubSub.Subscribe<MemoryWriteEvent>(
            MemoryWriteEvent.Topic,
            e => history.OnMemoryWrite(e.Address, e.OldValue));
        pubSub.Subscribe<CpuInstructionCompletedEvent>(
            CpuInstructionCompletedEvent.Topic,
            history.OnInstructionCompleted);
    }

    private static void DrainCompletedFrames(EmulatorRuntimeSession session, FrameBookkeeping frame)
    {
        var pending = frame.PendingFrames;
        if (pending <= 0)
            return;

        frame.PendingFrames -= pending;
        for (var i = 0; i < pending; i++)
        {
            session.RecordFrame();
            session.AdvanceHostAutomationFrame();
        }
    }

    private sealed class FrameBookkeeping
    {
        public bool Subscribed;
        public bool HistorySubscribed;
        public int PendingFrames;
        public IVideoChip? VideoChip;
        public EventHandler? FrameHandler;
    }
}
