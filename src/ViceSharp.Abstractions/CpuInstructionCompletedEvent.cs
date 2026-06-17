namespace ViceSharp.Abstractions;

/// <summary>
/// Published once per completed CPU instruction (at the instruction boundary, after
/// the opcode has fully executed) for diagnostic / pacing subscribers. Carries the
/// instruction's fetch address, its opcode, and the post-execution register file
/// (A/X/Y/S/P and PC). An unmanaged value type so it rides the
/// zero-allocation <see cref="IPubSub"/> hot path; the CPU only publishes when at
/// least one subscriber is listening (see <see cref="IPubSub.SubscriptionCount"/>),
/// so an unobserved run pays only a null/count check per instruction.
/// </summary>
public readonly record struct CpuInstructionCompletedEvent(
    ushort InstructionAddress,
    byte Opcode,
    byte A,
    byte X,
    byte Y,
    byte S,
    byte P,
    ushort Pc)
{
    /// <summary>Pub/Sub topic used for completed-instruction notifications.</summary>
    public static readonly Topic Topic = Topic.FromName("cpu.instruction-completed");
}
