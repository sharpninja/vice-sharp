namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Xbox.RomProvisioning;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S40 (IMPL-XBOXUWP-040), area XROM. FR-XROM-002, TR-XPATH-001. Guards the
/// head's concrete <see cref="RomFetchRomAcquirer"/>, whose whole reason to exist is to write the
/// verified core-ROM bytes under the CANONICAL VICE file names. RomFetch's
/// <c>RomProvider.DownloadRom</c> writes bare-keyed files (<c>basic</c>/<c>kernal</c>/
/// <c>characters</c>) that neither <see cref="RomProvisionEvaluator"/> nor the C64 machine's
/// C64RomSet can see, so a naive delegation would download successfully yet leave <c>c64</c>
/// unregistered. These tests inject the byte fetch (no network) and prove the canonical mapping +
/// that the written set makes the real evaluator report <see cref="RomProvisionState.Complete"/>.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class RomFetchRomAcquirerTests
{
    [Fact]
    public void CoreRoms_MapEachRoleToCanonicalCatalogFileName()
    {
        Assert.Equal("C64", RomFetchRomAcquirer.Architecture);

        var core = RomFetchRomAcquirer.CoreRoms;
        Assert.Equal(3, core.Count);

        // The canonical file name for each role MUST equal the production catalog's name (the
        // exact name the evaluator + the C64 machine read). This fails if anyone regresses the
        // acquirer to DownloadRom's bare keys.
        foreach (var rom in core)
            Assert.Equal(RomCatalog.C64.GetSpec(rom.Role).FileName, rom.CanonicalFileName);

        Assert.Equal("basic", Key(RomRole.Basic));
        Assert.Equal("kernal", Key(RomRole.Kernal));
        Assert.Equal("characters", Key(RomRole.Chargen));

        static string Key(RomRole role)
        {
            foreach (var rom in RomFetchRomAcquirer.CoreRoms)
            {
                if (rom.Role == role)
                    return rom.DownloadKey;
            }

            throw new Xunit.Sdk.XunitException($"No CoreRom for role {role}.");
        }
    }

    [Fact]
    public async Task DownloadCoreSetAsync_WritesVerifiedBytesUnderCanonicalNames()
    {
        var ct = TestContext.Current.CancellationToken;
        var dir = NewTempDir();
        try
        {
            var acquirer = new RomFetchRomAcquirer((key, arch, _) =>
            {
                Assert.Equal("C64", arch);
                var role = RoleForKey(key);
                return Task.FromResult<ReadOnlyMemory<byte>>(RomProvisionTestData.ValidBytes(role));
            });

            var result = await acquirer.DownloadCoreSetAsync(dir, ct);

            Assert.True(result.IsSuccess);

            foreach (var role in new[] { RomRole.Basic, RomRole.Kernal, RomRole.Chargen })
            {
                var path = Path.Combine(dir, RomCatalog.C64.GetSpec(role).FileName);
                Assert.True(File.Exists(path), $"Expected canonical ROM at '{path}'.");
                Assert.Equal(RomProvisionTestData.ValidBytes(role), await File.ReadAllBytesAsync(path, ct));
            }

            // The written set must satisfy the REAL evaluator end to end (this is what makes a
            // fresh factory scan register c64) - proven against the synthetic catalog whose specs
            // match the deterministic bytes and use the same canonical file names.
            var assessment = new RomProvisionEvaluator(RomProvisionTestData.Catalog)
                .Evaluate(dir, RomProfile.Standard);

            Assert.Equal(RomProvisionState.Complete, assessment.State);
            Assert.False(assessment.IsBootBlocked);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public async Task DownloadCoreSetAsync_FetchThrows_Propagates_AndWritesNothing()
    {
        var dir = NewTempDir();
        try
        {
            var acquirer = new RomFetchRomAcquirer((_, _, _) =>
                throw new HttpRequestException("simulated offline"));

            await Assert.ThrowsAsync<HttpRequestException>(
                () => acquirer.DownloadCoreSetAsync(dir, TestContext.Current.CancellationToken));

            // The first role failed before any byte was written, so no canonical file exists and
            // the VM's DownloadAsync catch degrades to the offline gate.
            Assert.Empty(Directory.GetFiles(dir));
        }
        finally
        {
            TryDelete(dir);
        }
    }

    private static RomRole RoleForKey(string key) => key switch
    {
        "basic" => RomRole.Basic,
        "kernal" => RomRole.Kernal,
        "characters" => RomRole.Chargen,
        _ => throw new Xunit.Sdk.XunitException($"Unexpected download key '{key}'."),
    };

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vicesharp-acq-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // Best-effort test cleanup.
        }
    }
}
