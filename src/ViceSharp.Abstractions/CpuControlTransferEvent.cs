namespace ViceSharp.Abstractions;

/// <summary>
/// CPU control-transfer notification published for diagnostic subscribers.
/// </summary>
public readonly record struct CpuControlTransferEvent(
    ushort Source,
    ushort Target,
    ushort ReturnPc,
    byte Opcode)
{
    /// <summary>
    /// Pub/Sub topic used for CPU control-transfer notifications.
    /// </summary>
    public static readonly Topic Topic = Topic.FromName("cpu.control-transfer");
}
