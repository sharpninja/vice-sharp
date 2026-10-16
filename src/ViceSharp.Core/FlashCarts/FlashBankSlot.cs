namespace ViceSharp.Core.FlashCarts;

/// <summary>One bank/window in a flash/ROM image under construction.</summary>
public sealed class FlashBankSlot
{
    public FlashBankSlot(int index, int offset, int length, string label)
    {
        Index = index;
        Offset = offset;
        Length = length;
        Label = label;
    }

    public int Index { get; }
    public int Offset { get; }
    public int Length { get; }
    public string Label { get; }
    public string? SourceName { get; set; }
    public FlashBankContentKind ContentKind { get; set; } = FlashBankContentKind.Empty;
}
