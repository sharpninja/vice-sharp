namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using ViceSharp.RomFetch;
using Xunit;

/// <summary>FR-INP-006 / TEST-INPUT-001: discover the desktop's shipped VICE maps.</summary>
public sealed class BundledKeyboardMapTests
{
    /// <summary>
    /// Use case: A fresh desktop installation has downloaded ROMs but no external
    /// VICE installation or user keymaps. Acceptance: the host enumerates every
    /// shipped C64 map from the application directory, including alternate layouts.
    /// </summary>
    [Fact]
    public async Task ListKeyboardMapsAsync_NoExternalVice_ListsBundledMaps()
    {
        var names = new[] { "VICESHARP_ROM_PATH", "VICE_DATA_PATH", "VICE_HOME", "PATH", "XDG_DATA_HOME" };
        var previous = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        var temporary = Path.Combine(Path.GetTempPath(), "ViceSharpBundledKeymaps", Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var name in names)
                Environment.SetEnvironmentVariable(name, null);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", temporary);
            // A discovered ROM-only root must not hide the packaged keymaps.
            var romRoot = Path.Combine(temporary, "roms");
            Directory.CreateDirectory(Path.Combine(romRoot, "C64"));
            Environment.SetEnvironmentVariable("VICESHARP_ROM_PATH", romRoot);

            var bundledRoot = Path.Combine(AppContext.BaseDirectory, "data");
            var roots = ViceDataPathResolver.FindDataRoots();
            Assert.Equal(romRoot, roots[0]);
            Assert.Equal(bundledRoot, roots[^1]);
            var files = Directory.GetFiles(Path.Combine(bundledRoot, "C64"), "*.vkm");
            Assert.True(files.Length > 1, "Alternate VICE keyboard layouts must be shipped.");

            var registry = new EmulatorRuntimeRegistry();
            var factory = new DefaultEmulatorRuntimeFactory(
                new ArchitectureBuilder(),
                [MinimalHostArchitectureDescriptor.Instance],
                MinimalHostArchitectureDescriptor.ArchitectureId);
            var session = factory.Create(new CreateEmulatorSessionRequest());
            registry.Add(session);
            var response = await new InputServiceHost(registry).ListKeyboardMapsAsync(
                new SessionRequest(session.SessionId), TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
            Assert.Equal(files.Length, response.KeyboardMaps.Count);
            Assert.Contains(response.KeyboardMaps, map => map.Id == "c64:gtk3_pos");
            Assert.Contains(response.KeyboardMaps, map => map.Id == "c64:gtk3_sym_de");
            Assert.Contains(response.KeyboardMaps, map => map.Id == "c64:sdl_sym_uk");
            Assert.All(response.KeyboardMaps, map => Assert.StartsWith(bundledRoot, map.SourcePath));
        }
        finally
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
            Directory.Delete(temporary, recursive: true);
        }
    }
}
