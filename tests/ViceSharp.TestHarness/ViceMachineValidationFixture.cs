using ViceSharp.Abstractions;

namespace ViceSharp.TestHarness;

internal sealed class ViceMachineValidationFixture : IAsyncDisposable
{
    public IMachine ManagedMachine { get; } = MachineTestFactory.CreateC64Machine();
    public IntPtr NativeMachine { get; private set; }

    public ValueTask InitializeAsync()
    {
        NativeMachine = ViceNativeBridge.CreateMachine();
        ViceNativeBridge.ResetMachine(NativeMachine);
        return ValueTask.CompletedTask;
    }

    public void ResetBoth()
    {
        ManagedMachine.Reset();
        ViceNativeBridge.ResetMachine(NativeMachine);
    }

    public void StepNative()
    {
        ViceNativeBridge.StepCycle(NativeMachine);
    }

    public void StepManaged()
    {
        ManagedMachine.Clock.Step();
    }

    public ValueTask DisposeAsync()
    {
        if (NativeMachine != IntPtr.Zero)
        {
            ViceNativeBridge.DestroyMachine(NativeMachine);
            NativeMachine = IntPtr.Zero;
        }

        return ValueTask.CompletedTask;
    }
}
