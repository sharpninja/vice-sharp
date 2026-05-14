using ViceSharp.Abstractions;

namespace ViceSharp.TestHarness;

internal sealed class ViceMachineValidationFixture : IAsyncDisposable
{
    private readonly string _modelSelector;

    public ViceMachineValidationFixture(string modelSelector = "c64")
    {
        _modelSelector = modelSelector;
        ManagedMachine = MachineTestFactory.CreateC64Machine(modelSelector);
    }

    public IMachine ManagedMachine { get; }
    public IntPtr NativeMachine { get; private set; }

    public ValueTask InitializeAsync()
    {
        NativeMachine = ViceNativeBridge.CreateMachine(_modelSelector);
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
