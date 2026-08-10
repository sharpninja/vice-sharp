namespace ViceSharp.Core.FlashCarts;

/// <summary>
/// Head-agnostic builder for banked flash/ROM cart images (FE3, Ultimem, Mega-Cart, EasyFlash layout).
/// </summary>
public sealed class FlashImageBuilder
{
    private readonly IFlashCartProfile _profile;
    private readonly byte[] _image;
    private readonly FlashBankSlot[] _banks;

    public FlashImageBuilder(IFlashCartProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _image = new byte[_profile.ImageSizeBytes];
        Array.Fill(_image, _profile.EmptyFill);
        _banks = new FlashBankSlot[_profile.BankCount];
        for (var i = 0; i < _banks.Length; i++)
        {
            var off = i * _profile.BankSizeBytes;
            _banks[i] = new FlashBankSlot(
                i,
                off,
                _profile.BankSizeBytes,
                $"Bank {i} ${off:X5}");
        }
    }

    public IFlashCartProfile Profile => _profile;

    public IReadOnlyList<FlashBankSlot> Banks => _banks;

    public void ClearAll()
    {
        Array.Fill(_image, _profile.EmptyFill);
        foreach (var bank in _banks)
        {
            bank.ContentKind = FlashBankContentKind.Empty;
            bank.SourceName = null;
        }
    }

    public void ClearBank(int bankIndex)
    {
        var bank = GetBank(bankIndex);
        Array.Fill(_image, _profile.EmptyFill, bank.Offset, bank.Length);
        bank.ContentKind = FlashBankContentKind.Empty;
        bank.SourceName = null;
    }

    /// <summary>
    /// Writes raw bytes into a bank. Throws if <paramref name="data"/> plus
    /// <paramref name="destOffset"/> exceeds the bank length.
    /// </summary>
    public void WriteRaw(int bankIndex, ReadOnlySpan<byte> data, int destOffset = 0, string? sourceName = null)
    {
        var bank = GetBank(bankIndex);
        if (destOffset < 0 || destOffset > bank.Length)
            throw new ArgumentOutOfRangeException(nameof(destOffset));
        if (data.Length > bank.Length - destOffset)
            throw new ArgumentException(
                $"Data length {data.Length} exceeds remaining bank capacity {bank.Length - destOffset}.",
                nameof(data));

        data.CopyTo(_image.AsSpan(bank.Offset + destOffset, data.Length));
        bank.ContentKind = FlashBankContentKind.Raw;
        bank.SourceName = sourceName ?? bank.SourceName ?? "raw";
    }

    /// <summary>
    /// Places a PRG payload into a bank after stripping a 2-byte load address when present.
    /// Policy: if length &gt;= 3, strip first two bytes as load address (standard CBM PRG).
    /// </summary>
    public void WritePrg(int bankIndex, ReadOnlySpan<byte> prg, string? sourceName = null)
    {
        if (prg.Length < 3)
            throw new ArgumentException("PRG image too short (need load address + data).", nameof(prg));

        var payload = prg[2..];
        WriteRaw(bankIndex, payload, destOffset: 0, sourceName: sourceName ?? "prg");
        GetBank(bankIndex).ContentKind = FlashBankContentKind.Prg;
    }

    public byte[] Build() => _image.ToArray();

    /// <summary>
    /// Secondary region (e.g. Mega-Cart NVRAM) filled with <see cref="IFlashCartProfile.EmptyFill"/>.
    /// Null when profile has no secondary.
    /// </summary>
    public byte[]? BuildSecondary()
    {
        if (_profile.SecondarySizeBytes <= 0)
            return null;
        var sec = new byte[_profile.SecondarySizeBytes];
        Array.Fill(sec, _profile.EmptyFill);
        return sec;
    }

    private FlashBankSlot GetBank(int bankIndex)
    {
        if ((uint)bankIndex >= (uint)_banks.Length)
            throw new ArgumentOutOfRangeException(nameof(bankIndex));
        return _banks[bankIndex];
    }
}
