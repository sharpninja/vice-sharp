namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Linq;
using ViceSharp.Core.Configuration;
using ViceSharp.Protocol;
using ViceSharp.Xbox.Platform;
using Xunit;

/// <summary>
/// FEAT-XDEFAULTCART-001 (PLAN-XBOXUWP, area XBOXMEDIA). Operator 2026-07-14: "Set
/// emulator to load sblox.CRT cartridge by default until user selects different media.
/// Use the vice.ini file as normal. sblox.CRT needs to be an embedded resource."
/// </summary>
/// <remarks>
/// The S-Blox cartridge ships EMBEDDED in the head assembly; on first boot the head
/// extracts it beside the ROMs and records it in the canonical vice.ini exactly the way
/// VICE does (<c>[C64] CartridgeFile</c> + <c>CartridgeType=0</c> via the Core
/// <see cref="ViceSettings"/> writer); every boot attaches whatever CartridgeFile says;
/// a user attaching a DIFFERENT cartridge updates the resource, and attaching non-cart
/// media (disk/tape) clears it so the default stops overriding their choice.
/// Acceptance:
///   TEST-XCART-001a: sblox.CRT is an embedded resource of the head assembly.
///   TEST-XCART-001b: first boot (no CartridgeFile in vice.ini) extracts the embedded
///     cartridge to the cartridge directory, writes CartridgeFile/CartridgeType into
///     vice.ini, and resolves that path; the extracted bytes carry the C64 CARTRIDGE
///     signature.
///   TEST-XCART-001c: an existing CartridgeFile is honored as-is (no re-extract, no
///     rewrite); a CartridgeFile pointing at a missing file resolves to nothing.
///   TEST-XCART-001d: user media selection updates vice.ini as normal: another
///     cartridge replaces CartridgeFile; disk/tape media clears it; detaching the
///     cartridge clears it.
///   TEST-XCART-001e (structural): the head embeds the resource in the csproj and the
///     boot + facade wiring call the policy.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxDefaultCartridgeTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "vicesharp-cart-tests", Guid.NewGuid().ToString("N"));

    public XboxDefaultCartridgeTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }

    [Fact]
    public void SbloxCartridge_IsAnEmbeddedResource()
    {
        // TEST-XCART-001a.
        var assembly = typeof(DefaultCartridgeBoot).Assembly;
        var name = assembly.GetManifestResourceNames()
            .SingleOrDefault(n => n.EndsWith("sblox.CRT", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(name);

        using var stream = assembly.GetManifestResourceStream(name!);
        Assert.NotNull(stream);
        var header = new byte[16];
        Assert.Equal(16, stream!.Read(header, 0, 16));
        Assert.StartsWith("C64 CARTRIDGE", System.Text.Encoding.ASCII.GetString(header));
    }

    [Fact]
    public void FirstBoot_ExtractsTheDefault_AndWritesViceIni()
    {
        // TEST-XCART-001b.
        var settings = ViceSettings.OpenAt(_root);
        var cartridgeDirectory = Path.Combine(_root, "C64");

        var resolved = DefaultCartridgeBoot.ResolveBootCartridge(settings, cartridgeDirectory);

        Assert.NotNull(resolved);
        Assert.True(File.Exists(resolved));
        Assert.StartsWith(
            "C64 CARTRIDGE",
            System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(resolved!), 0, 13));

        // vice.ini carries the canonical resources, exactly as VICE writes them.
        var reopened = ViceSettings.OpenAt(_root);
        Assert.Equal(resolved, reopened.Get("C64", "CartridgeFile"));
        Assert.Equal("0", reopened.Get("C64", "CartridgeType"));
    }

    [Fact]
    public void ExistingCartridgeFile_IsHonored_AndMissingFileResolvesToNothing()
    {
        // TEST-XCART-001c: a user-chosen cartridge stays untouched.
        var settings = ViceSettings.OpenAt(_root);
        var userCart = Path.Combine(_root, "user-cart.crt");
        File.WriteAllBytes(userCart, new byte[] { 1, 2, 3 });
        settings.SetVice("C64", "CartridgeFile", userCart);
        settings.Save();

        var resolved = DefaultCartridgeBoot.ResolveBootCartridge(settings, Path.Combine(_root, "C64"));
        Assert.Equal(userCart, resolved);

        // Pointing at a vanished file resolves to nothing (no forced re-default).
        settings.SetVice("C64", "CartridgeFile", Path.Combine(_root, "gone.crt"));
        settings.Save();
        Assert.Null(DefaultCartridgeBoot.ResolveBootCartridge(settings, Path.Combine(_root, "C64")));
    }

    [Fact]
    public void UserMediaSelection_UpdatesViceIni_AsNormal()
    {
        // TEST-XCART-001d.
        var settings = ViceSettings.OpenAt(_root);
        var cartridgeDirectory = Path.Combine(_root, "C64");
        DefaultCartridgeBoot.ResolveBootCartridge(settings, cartridgeDirectory);

        // Another cartridge replaces the resource.
        var otherCart = Path.Combine(_root, "other.crt");
        DefaultCartridgeBoot.NoteUserMediaSelection(settings, MediaSlot.Cartridge, otherCart);
        Assert.Equal(otherCart, ViceSettings.OpenAt(_root).Get("C64", "CartridgeFile"));

        // Non-cartridge media clears it: the default must stop overriding the choice.
        DefaultCartridgeBoot.NoteUserMediaSelection(settings, MediaSlot.Drive8, Path.Combine(_root, "game.d64"));
        Assert.True(string.IsNullOrEmpty(ViceSettings.OpenAt(_root).Get("C64", "CartridgeFile")));

        // Detaching a cartridge (null path) also clears it.
        DefaultCartridgeBoot.NoteUserMediaSelection(settings, MediaSlot.Cartridge, otherCart);
        DefaultCartridgeBoot.NoteUserMediaSelection(settings, MediaSlot.Cartridge, null);
        Assert.True(string.IsNullOrEmpty(ViceSettings.OpenAt(_root).Get("C64", "CartridgeFile")));
    }

    [Fact]
    public void Head_WiresTheDefaultCartridge_AtBootAndOnUserAttach()
    {
        // TEST-XCART-001e.
        var csproj = File.ReadAllText(Path.Combine(
            RepoRoot, "src", "ViceSharp.Xbox", "ViceSharp.Xbox.csproj"));
        Assert.Contains("EmbeddedResource", csproj);
        Assert.Contains("sblox.CRT", csproj);

        var app = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "App.xaml.cs"))
            .ToLowerInvariant();
        Assert.Contains("resolvebootcartridge", app);
        Assert.Contains("mediaselectionchanged", app);

        var facade = File.ReadAllText(Path.Combine(
                RepoRoot, "src", "ViceSharp.Xbox", "Platform", "InProcessSessionFacade.cs"))
            .ToLowerInvariant();
        Assert.Contains("mediaselectionchanged", facade);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
