using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Runtime holder for the board-level policy selected by a machine profile.
/// ArchitectureBuilder creates the chips and connects them through this policy.
/// </summary>
internal sealed class SystemCore : ISystemCore
{
    public SystemCore(ISystemCoreDefinition definition)
    {
        Definition = definition;
    }

    public DeviceId Id => new(0x0100);

    public string Name => Definition.DisplayName;

    public ISystemCoreDefinition Definition { get; }

    public void Reset()
    {
    }
}
