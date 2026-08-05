namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. Off-console test data for the
/// first-run ROM provisioning suite (<c>RomProvisionEvaluatorTests</c> /
/// <c>XboxRomProvisioningViewModelTests</c>).
///
/// <para>
/// ViceSharp ships NO Commodore ROMs and the real VICE dumps cannot live in the
/// repository, so the off-console suite cannot use the production
/// <see cref="RomCatalog.C64"/> (real SHA256/MD5 pins) with real bytes. Instead it
/// injects <see cref="Catalog"/>: a synthetic catalog whose per-role specs carry the
/// hashes/sizes of deterministic synthetic byte-sets this helper also writes to disk.
/// The evaluator/ViewModel logic under test is identical; only the pinned digests are
/// swapped for ones we can reproduce without shipping copyrighted ROM data. A separate
/// parity test asserts <see cref="RomCatalog.C64"/> carries the real VICE pins.
/// </para>
/// </summary>
internal static class RomProvisionTestData
{
    /// <summary>The real VICE core-C64 file names (parity with RomProvider.cs / C64RomLoader.cs).</summary>
    public const string BasicFileName = "basic-901226-01.bin";

    /// <summary>The real VICE KERNAL rev-3 dump file name.</summary>
    public const string KernalFileName = "kernal-901227-03.bin";

    /// <summary>The real VICE character-generator dump file name.</summary>
    public const string ChargenFileName = "chargen-901225-01.bin";

    private static readonly byte[] BasicBytes = Synthesize(RomRole.Basic, 8192);
    private static readonly byte[] KernalBytes = Synthesize(RomRole.Kernal, 8192);
    private static readonly byte[] ChargenBytes = Synthesize(RomRole.Chargen, 4096);

    /// <summary>
    /// A synthetic catalog whose per-role specs match the deterministic byte-sets this
    /// helper writes (size + SHA256 + MD5), so a written "valid" file classifies as
    /// <c>Present</c> under the evaluator without any real ROM data.
    /// </summary>
    public static RomCatalog Catalog { get; } = new RomCatalog(new[]
    {
        SpecFor(RomRole.Basic, BasicFileName, BasicBytes),
        SpecFor(RomRole.Kernal, KernalFileName, KernalBytes),
        SpecFor(RomRole.Chargen, ChargenFileName, ChargenBytes),
    });

    /// <summary>The deterministic "valid" bytes for a role (matching <see cref="Catalog"/>).</summary>
    public static byte[] ValidBytes(RomRole role) => role switch
    {
        RomRole.Basic => (byte[])BasicBytes.Clone(),
        RomRole.Kernal => (byte[])KernalBytes.Clone(),
        RomRole.Chargen => (byte[])ChargenBytes.Clone(),
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    /// <summary>The catalog file name for a role.</summary>
    public static string FileName(RomRole role) => Catalog.GetSpec(role).FileName;

    /// <summary>Writes the deterministic valid file for a single role into <paramref name="c64Directory"/>.</summary>
    public static void WriteValid(string c64Directory, RomRole role)
    {
        Directory.CreateDirectory(c64Directory);
        File.WriteAllBytes(Path.Combine(c64Directory, FileName(role)), ValidBytes(role));
    }

    /// <summary>Writes the deterministic valid files for all three core roles.</summary>
    public static void WriteValidSet(string c64Directory)
    {
        WriteValid(c64Directory, RomRole.Basic);
        WriteValid(c64Directory, RomRole.Kernal);
        WriteValid(c64Directory, RomRole.Chargen);
    }

    /// <summary>
    /// Writes a correctly-named, correctly-SIZED file for a role whose bytes do NOT match
    /// the spec digest (so the evaluator classifies it <c>Invalid</c>).
    /// </summary>
    public static void WriteWrongBytes(string c64Directory, RomRole role)
    {
        Directory.CreateDirectory(c64Directory);
        var corrupt = new byte[Catalog.GetSpec(role).ExpectedSize];
        // All-zero bytes of the correct size: size matches, hash does not.
        File.WriteAllBytes(Path.Combine(c64Directory, FileName(role)), corrupt);
    }

    /// <summary>A picked file whose bytes are the deterministic valid set for a role.</summary>
    public static PickedFile PickedValid(RomRole role)
    {
        var bytes = ValidBytes(role);
        return new PickedFile(FileName(role), @"C:\usb\" + FileName(role), bytes.Length,
            _ => Task.FromResult(bytes));
    }

    /// <summary>
    /// A picked file with the correct SIZE but wrong bytes (all-zero), so an import
    /// passes the size gate and is rejected on the MD5 gate.
    /// </summary>
    public static PickedFile PickedWrongBytes(RomRole role)
    {
        var size = Catalog.GetSpec(role).ExpectedSize;
        var bytes = new byte[size];
        return new PickedFile(FileName(role), @"C:\usb\" + FileName(role), size,
            _ => Task.FromResult(bytes));
    }

    /// <summary>
    /// A picked file that REPORTS a length above the 64MB import ceiling. Its byte reader
    /// throws, so a correct implementation (which rejects on the reported length BEFORE
    /// reading) never trips it.
    /// </summary>
    public static PickedFile PickedOversize(RomRole role) =>
        new PickedFile(
            FileName(role),
            @"C:\usb\" + FileName(role),
            XboxRomProvisioningViewModel.MaxImportBytes + 1,
            _ => throw new InvalidOperationException(
                "Oversize picked file must be rejected before its bytes are read."));

    private static RomSpec SpecFor(RomRole role, string fileName, byte[] bytes) =>
        new RomSpec(
            role,
            fileName,
            bytes.Length,
            Convert.ToHexString(SHA256.HashData(bytes)),
            Convert.ToHexString(MD5.HashData(bytes)));

    private static byte[] Synthesize(RomRole role, int size)
    {
        var bytes = new byte[size];
        var seed = role switch
        {
            RomRole.Basic => 0x11,
            RomRole.Kernal => 0x22,
            RomRole.Chargen => 0x33,
            _ => 0x44,
        };

        for (var i = 0; i < size; i++)
        {
            bytes[i] = (byte)(((i * 31) + seed) & 0xFF);
        }

        return bytes;
    }
}
