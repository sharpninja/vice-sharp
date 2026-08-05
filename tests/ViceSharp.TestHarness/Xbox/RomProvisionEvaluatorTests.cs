namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Linq;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. TEST-XSET-001 off-console coverage for
/// <see cref="RomProvisionEvaluator"/>: the pure first-run ROM-provisioning classifier that
/// reads the C64 ROM directory and reports per-role presence + overall
/// <see cref="RomProvisionState"/> + <c>IsBootBlocked</c>.
///
/// <para>
/// ViceSharp ships no ROMs, so every test injects the synthetic
/// <see cref="RomProvisionTestData.Catalog"/> (hashes of reproducible byte-sets) rather
/// than the real <see cref="RomCatalog.C64"/> pins; a dedicated parity test guards that
/// <see cref="RomCatalog.C64"/> still carries the real VICE names/sizes/digests copied from
/// <c>RomProvider.cs:127-129</c> + <c>C64RomLoader.cs:13-80</c>. Plain
/// <see cref="FactAttribute"/> (no console gate).
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class RomProvisionEvaluatorTests
{
    private static string NewTempC64Directory() =>
        Path.Combine(Path.GetTempPath(), "vicesharp-xbox-rom-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// FR-XROM-001, TR-XPATH-001. TEST-XSET-001.
    /// Use case: on a fresh device with no ROMs provisioned, the wizard must block normal boot.
    /// Acceptance: an empty (or absent) C64 directory evaluates to
    /// <see cref="RomProvisionState.NotProvisioned"/>, every role is <see cref="RomPresence.Missing"/>,
    /// and <c>IsBootBlocked</c> is true.
    /// </summary>
    [Fact]
    public void Evaluate_NoRomFiles_NotProvisionedAndBootBlocked()
    {
        var directory = NewTempC64Directory();
        try
        {
            var evaluator = new RomProvisionEvaluator(RomProvisionTestData.Catalog);

            var assessment = evaluator.Evaluate(directory, RomProfile.Standard);

            Assert.Equal(RomProvisionState.NotProvisioned, assessment.State);
            Assert.True(assessment.IsBootBlocked);
            Assert.All(assessment.Roles, role => Assert.Equal(RomPresence.Missing, role.Presence));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-001, TR-XPATH-001. TEST-XSET-001.
    /// Use case: once all three core ROMs are present and hash-valid the machine may boot.
    /// Acceptance: a directory with the three correctly-sized, correctly-hashed core ROMs
    /// evaluates to <see cref="RomProvisionState.Complete"/> with <c>IsBootBlocked</c> false.
    /// </summary>
    [Fact]
    public void Evaluate_AllThreeValid_CompleteAndNotBootBlocked()
    {
        var directory = NewTempC64Directory();
        try
        {
            RomProvisionTestData.WriteValidSet(directory);
            var evaluator = new RomProvisionEvaluator(RomProvisionTestData.Catalog);

            var assessment = evaluator.Evaluate(directory, RomProfile.Standard);

            Assert.Equal(RomProvisionState.Complete, assessment.State);
            Assert.False(assessment.IsBootBlocked);
            Assert.All(assessment.Roles, role => Assert.Equal(RomPresence.Present, role.Presence));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-001, FR-XROM-002, TR-XPATH-001. TEST-XSET-001.
    /// Use case: a correctly-named ROM whose bytes do not match the pinned digest is a
    /// corrupt/wrong dump and must not be trusted.
    /// Acceptance: a correctly-sized file with wrong bytes classifies that role
    /// <see cref="RomPresence.Invalid"/> and drives overall <see cref="RomProvisionState.Invalid"/>
    /// with <c>IsBootBlocked</c> true.
    /// </summary>
    [Fact]
    public void Evaluate_CorrectNameWrongBytes_Invalid()
    {
        var directory = NewTempC64Directory();
        try
        {
            RomProvisionTestData.WriteValid(directory, RomRole.Basic);
            RomProvisionTestData.WriteValid(directory, RomRole.Chargen);
            RomProvisionTestData.WriteWrongBytes(directory, RomRole.Kernal);
            var evaluator = new RomProvisionEvaluator(RomProvisionTestData.Catalog);

            var assessment = evaluator.Evaluate(directory, RomProfile.Standard);

            Assert.Equal(RomProvisionState.Invalid, assessment.State);
            Assert.True(assessment.IsBootBlocked);
            var kernal = assessment.Roles.Single(role => role.Role == RomRole.Kernal);
            Assert.Equal(RomPresence.Invalid, kernal.Presence);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-001, TR-XPATH-001. TEST-XSET-001.
    /// Use case: a half-imported set (some present, none invalid) should leave the wizard
    /// in a resumable partial state, still blocking boot.
    /// Acceptance: two of three valid ROMs present (third missing) evaluates to
    /// <see cref="RomProvisionState.Partial"/> with <c>IsBootBlocked</c> true.
    /// </summary>
    [Fact]
    public void Evaluate_TwoOfThreePresent_Partial()
    {
        var directory = NewTempC64Directory();
        try
        {
            RomProvisionTestData.WriteValid(directory, RomRole.Basic);
            RomProvisionTestData.WriteValid(directory, RomRole.Kernal);
            // Chargen deliberately absent.
            var evaluator = new RomProvisionEvaluator(RomProvisionTestData.Catalog);

            var assessment = evaluator.Evaluate(directory, RomProfile.Standard);

            Assert.Equal(RomProvisionState.Partial, assessment.State);
            Assert.True(assessment.IsBootBlocked);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-001, TR-XPATH-001. TEST-XSET-001.
    /// Use case: an Ultimax-profile cartridge overrides the KERNAL, so KERNAL absence must
    /// not block boot (mirrors <c>C64RomLoader.cs:192-193</c> kernal-none handling).
    /// Acceptance: under <see cref="RomProfile.Ultimax"/>, BASIC + CHARGEN present with no
    /// KERNAL still evaluates <see cref="RomProvisionState.Complete"/> and <c>IsBootBlocked</c> false.
    /// </summary>
    [Fact]
    public void Evaluate_UltimaxProfileWithoutKernal_NotBootBlocked()
    {
        var directory = NewTempC64Directory();
        try
        {
            RomProvisionTestData.WriteValid(directory, RomRole.Basic);
            RomProvisionTestData.WriteValid(directory, RomRole.Chargen);
            // KERNAL deliberately absent; optional under Ultimax.
            var evaluator = new RomProvisionEvaluator(RomProvisionTestData.Catalog);

            var assessment = evaluator.Evaluate(directory, RomProfile.Ultimax);

            Assert.Equal(RomProvisionState.Complete, assessment.State);
            Assert.False(assessment.IsBootBlocked);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-002, TR-XPATH-001. TEST-XSET-001.
    /// Use case: the shipped production catalog must carry the exact VICE core-C64 names,
    /// sizes and digests (SHA256 from <c>RomProvider.cs:127-129</c>, MD5 + size from
    /// <c>C64RomLoader.cs:13-80</c>) so download-verification and import-validation match VICE.
    /// Acceptance: <see cref="RomCatalog.C64"/> exposes basic/kernal/chargen with the pinned
    /// file names, sizes (8192/8192/4096), SHA256 and MD5 hex digests.
    /// </summary>
    [Fact]
    public void C64Catalog_MatchesRomProviderAndLoaderPins()
    {
        var basic = RomCatalog.C64.GetSpec(RomRole.Basic);
        Assert.Equal("basic-901226-01.bin", basic.FileName);
        Assert.Equal(8192, basic.ExpectedSize);
        Assert.Equal("89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D", basic.ExpectedSha256, ignoreCase: true);
        Assert.Equal("57af4ae21d4b705c2991d98ed5c1f7b8", basic.ExpectedMd5, ignoreCase: true);

        var kernal = RomCatalog.C64.GetSpec(RomRole.Kernal);
        Assert.Equal("kernal-901227-03.bin", kernal.FileName);
        Assert.Equal(8192, kernal.ExpectedSize);
        Assert.Equal("83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721", kernal.ExpectedSha256, ignoreCase: true);
        Assert.Equal("39065497630802346bce17963f13c092", kernal.ExpectedMd5, ignoreCase: true);

        var chargen = RomCatalog.C64.GetSpec(RomRole.Chargen);
        Assert.Equal("chargen-901225-01.bin", chargen.FileName);
        Assert.Equal(4096, chargen.ExpectedSize);
        Assert.Equal("FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420", chargen.ExpectedSha256, ignoreCase: true);
        Assert.Equal("12a4202f5331d45af846af6c58fba946", chargen.ExpectedMd5, ignoreCase: true);
    }
}
