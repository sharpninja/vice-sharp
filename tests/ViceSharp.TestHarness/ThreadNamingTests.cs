namespace ViceSharp.TestHarness;

using System.Reflection;
using ViceSharp.Core.Media;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using Xunit;

/// <summary>
/// Regression coverage for debugger-visible names on dedicated worker threads.
/// </summary>
public sealed class ThreadNamingTests
{
    /// <summary>
    /// FR: FR-Host-Diagnostics, TR: TR-THREAD-NAME-001.
    /// Use case: The host pump owns a dedicated emulation worker, so debugger
    /// thread lists must identify it by role instead of showing an anonymous
    /// .NET thread.
    /// Acceptance: Starting the pump creates a background thread named
    /// ViceSharp.Emulation.Pump.
    /// </summary>
    [Fact]
    public async Task EmulationPumpService_NamesWorkerThread()
    {
        using var pump = new EmulationPumpService(new EmulatorRuntimeRegistry(), new IdleGate());

        await pump.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var worker = GetPrivateThread(pump, "_workerThread");

            Assert.NotNull(worker);
            Assert.Equal("ViceSharp.Emulation.Pump", worker!.Name);
            Assert.True(worker.IsBackground);
        }
        finally
        {
            await pump.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// FR: FR-Host-Diagnostics, TR: TR-THREAD-NAME-001.
    /// Use case: The semaphore pacing strategy owns a high-resolution timer
    /// worker, so debugger thread lists must distinguish it from the emulation
    /// pump.
    /// Acceptance: Starting the gate creates a background thread named
    /// ViceSharp.Emulation.SemaphoreTimer.
    /// </summary>
    [Fact]
    public void SemaphoreEmulationGate_NamesTimerThread()
    {
        using var gate = new SemaphoreEmulationGate();

        gate.Start();
        try
        {
            var timer = GetPrivateThread(gate, "_timerThread");

            Assert.NotNull(timer);
            Assert.Equal("ViceSharp.Emulation.SemaphoreTimer", timer!.Name);
            Assert.True(timer.IsBackground);
        }
        finally
        {
            gate.Stop();
        }
    }

    /// <summary>
    /// FR: FR-Host-Diagnostics, TR: TR-THREAD-NAME-001.
    /// Use case: Media capture writes run on dedicated background threads; each
    /// caller-supplied name must be preserved for dump and debugger diagnosis.
    /// Acceptance: BackgroundByteWriter applies the provided name to its worker
    /// thread.
    /// </summary>
    [Fact]
    public void BackgroundByteWriter_AppliesProvidedThreadName()
    {
        using var writer = new BackgroundByteWriter((_, _) => { }, capacity: 1, name: "ViceSharp.Test.Writer");

        var worker = GetPrivateThread(writer, "_worker");

        Assert.NotNull(worker);
        Assert.Equal("ViceSharp.Test.Writer", worker!.Name);
        Assert.True(worker.IsBackground);
    }

    private static Thread? GetPrivateThread(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (Thread?)field!.GetValue(instance);
    }

    private sealed class IdleGate : IEmulationGate
    {
        public string Name => "Test";

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public bool Tick(EmulatorRuntimeRegistry registry, Func<EmulatorRuntimeSession, long, long> advance)
            => false;

        public void Dispose()
        {
        }
    }
}
