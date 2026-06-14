using ViceSharp.Chips.IEC;

namespace ViceSharp.Core;

public sealed class IecD64Attachment
{
    private readonly IecDrive _drive;

    private IecD64Attachment(byte driveNumber, D64Image image)
    {
        DriveNumber = driveNumber;
        Image = image;
        _drive = new IecDrive(driveNumber, image);
    }

    public byte DriveNumber { get; }

    public D64Image Image { get; }

    public static bool TryAttach(byte driveNumber, ReadOnlySpan<byte> imageData, out IecD64Attachment? attachment)
    {
        if (driveNumber is < 8 or > 11 || imageData.Length != D64Image.DiskSize35Track)
        {
            attachment = null;
            return false;
        }

        attachment = new IecD64Attachment(driveNumber, new D64Image(imageData.ToArray()));
        return true;
    }

    public bool TryReadSector(int track, int sector, Span<byte> destination)
    {
        if (destination.Length < 256 || !IsValidSector(track, sector) || !_drive.ReadSector(track, sector))
        {
            return false;
        }

        for (var offset = 0; offset < 256; offset++)
        {
            destination[offset] = _drive.Read((ushort)offset);
        }

        return true;
    }

    private static bool IsValidSector(int track, int sector)
    {
        var sectorCount = track switch
        {
            >= 1 and <= 17 => 21,
            >= 18 and <= 24 => 19,
            >= 25 and <= 30 => 18,
            >= 31 and <= 35 => 17,
            _ => 0
        };

        return sector >= 0 && sector < sectorCount;
    }
}
