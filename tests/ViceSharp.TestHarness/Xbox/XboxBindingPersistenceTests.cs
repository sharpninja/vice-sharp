namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S12 (IMPL-XBOXUWP-012). TEST-SYSBTN-003: remappable binding
/// PERSISTENCE for the Xbox-on-console head, built on the pure input model
/// (<see cref="BindingProfile"/>, <see cref="ButtonBinding"/>) and the pure
/// evaluator (<see cref="XboxSystemButtons"/>) from S10.
/// </summary>
/// <remarks>
/// <para>
/// The persistence path is AOT-safe and reflection-free: serialization goes through
/// the System.Text.Json SOURCE-GENERATED <see cref="BindingJsonContext"/> only
/// (always the <c>JsonTypeInfo</c> overload, never a reflective generic overload).
/// The store (<see cref="InMemoryBindingStore"/>) round-trips real JSON on
/// Save/Load, so these tests exercise the actual serializer, not a mock.
/// </para>
/// <para>
/// A single resolved-config flow (<see cref="XboxInputConfigResolver"/> over
/// <see cref="XboxInputPrefs"/>) produces ONE <see cref="XboxInputConfig"/> that
/// feeds the converter (<see cref="StickConverter"/>) and mapper
/// (<see cref="XboxJoystickMapper"/>); the binding profile file
/// (<c>bindings.v1.json</c>) is referenced BY the prefs via
/// <see cref="XboxInputPrefs.BindingProfilePath"/> rather than being a second,
/// competing config store for the converter. The INI/file wiring itself is a later
/// slice (S29); these tests are portable and in-memory.
/// </para>
/// <para>
/// Records with an <see cref="IReadOnlyList{T}"/> member (<see cref="BindingProfile"/>)
/// compare that member by reference under the compiler-generated equality, so these
/// tests assert VALUE equality with an element-wise helper
/// (<see cref="AssertProfilesEqual"/>), mirroring the S10 default-table guard.
/// </para>
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxBindingPersistenceTests
{
    private static GamepadSnapshot Buttons(GamepadButtonFlags buttons) =>
        new(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, buttons, 0UL);

    private static string Serialize(BindingProfile profile) =>
        JsonSerializer.Serialize(profile, BindingJsonContext.Default.BindingProfile);

    private static BindingProfile Deserialize(string json) =>
        JsonSerializer.Deserialize(json, BindingJsonContext.Default.BindingProfile)
        ?? throw new InvalidOperationException("Deserialize returned null.");

    private static void AssertProfilesEqual(BindingProfile expected, BindingProfile actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.DisplayName, actual.DisplayName);
        // ButtonBinding is a record: SequenceEqual compares each row by value.
        Assert.True(
            expected.Gameplay.SequenceEqual(actual.Gameplay),
            "Gameplay rows differ by value:\n" +
            "  expected: " + string.Join(", ", expected.Gameplay) + "\n" +
            "  actual:   " + string.Join(", ", actual.Gameplay));
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-001 (IMPL-XBOXUWP-012), TEST-SYSBTN-003 round-trip guard.
    /// Use case: an operator's edited control scheme must persist and reload exactly,
    /// with a stable on-disk form so a re-save does not churn the file.
    /// Acceptance: an edited profile Save-then-Load is value-equal to the saved
    /// profile, its JSON re-serializes byte-identically after a load, and a second
    /// serialize-&gt;deserialize-&gt;serialize is idempotent.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void EditedProfile_RoundTripsByteIdentical_ThroughContextAndStore()
    {
        // An edited scheme: replace an existing row (X) and bind a previously-unbound
        // input (RightTrigger) so both the replace and add remap paths are covered.
        var edited = BindingProfile.Default
            .WithBinding(BindableInput.X, AppCommand.ColdReset, BindingActivation.Press)
            .WithBinding(BindableInput.RightTrigger, AppCommand.ToggleWarp, BindingActivation.Toggle);

        string json1 = Serialize(edited);

        var store = new InMemoryBindingStore();
        store.Save(edited);
        BindingProfile loaded = store.Load();

        // Value-equal (element-wise): Save/Load preserved every row.
        AssertProfilesEqual(edited, loaded);

        // Byte-identical: re-serializing the loaded profile reproduces the saved JSON.
        string json2 = Serialize(loaded);
        Assert.Equal(json1, json2);

        // Idempotent: deserialize-then-serialize is stable.
        string json3 = Serialize(Deserialize(json2));
        Assert.Equal(json2, json3);
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-001 (IMPL-XBOXUWP-012), TEST-SYSBTN-003 reset guard.
    /// Use case: an operator can restore the locked default control scheme; a fresh
    /// store with nothing saved already reports the default.
    /// Acceptance: after saving an edit then <c>ResetToDefaults</c>, Load is value-equal
    /// to <see cref="BindingProfile.Default"/>; a brand-new store Loads the default too.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ResetToDefaults_YieldsDefaultProfile()
    {
        var store = new InMemoryBindingStore();
        store.Save(BindingProfile.Default.WithBinding(
            BindableInput.X, AppCommand.ColdReset, BindingActivation.Press));

        store.ResetToDefaults();
        AssertProfilesEqual(BindingProfile.Default, store.Load());

        // Nothing saved -> Default.
        var fresh = new InMemoryBindingStore();
        AssertProfilesEqual(BindingProfile.Default, fresh.Load());
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-002 (IMPL-XBOXUWP-012), TEST-SYSBTN-003 remap guard.
    /// Use case: remapping a bindable input changes the command the evaluator emits on
    /// that input's edge, and the remap survives a persistence round-trip.
    /// Acceptance: Default emits AutostartDrive8 on an X down edge; after remapping X to
    /// ColdReset the same edge emits ColdReset (not AutostartDrive8); the remap produces
    /// the same result after Save/Load through the store.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Remap_ChangesProducedAppCommand()
    {
        var config = XboxInputConfig.Default;
        var latch = SystemButtonLatch.Initial;
        var none = Buttons(GamepadButtonFlags.None);
        var xHeld = Buttons(GamepadButtonFlags.X);

        // Baseline: Default maps X -> AutostartDrive8.
        var baseOut = new List<AppCommand>();
        XboxSystemButtons.Evaluate(none, xHeld, BindingProfile.Default, config, latch, baseOut);
        Assert.Contains(AppCommand.AutostartDrive8, baseOut);
        Assert.DoesNotContain(AppCommand.ColdReset, baseOut);

        // Remap X -> ColdReset: the SAME edge now produces ColdReset instead.
        var remapped = BindingProfile.Default.WithBinding(
            BindableInput.X, AppCommand.ColdReset, BindingActivation.Press);

        var remapOut = new List<AppCommand>();
        XboxSystemButtons.Evaluate(none, xHeld, remapped, config, latch, remapOut);
        Assert.Contains(AppCommand.ColdReset, remapOut);
        Assert.DoesNotContain(AppCommand.AutostartDrive8, remapOut);

        // The remap survives persistence: Save/Load then re-evaluate is still ColdReset.
        var store = new InMemoryBindingStore();
        store.Save(remapped);
        BindingProfile persisted = store.Load();

        var persistOut = new List<AppCommand>();
        XboxSystemButtons.Evaluate(none, xHeld, persisted, config, latch, persistOut);
        Assert.Contains(AppCommand.ColdReset, persistOut);
        Assert.DoesNotContain(AppCommand.AutostartDrive8, persistOut);
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-XBOXAOT-001 (IMPL-XBOXUWP-012), TEST-SYSBTN-003 no-reflection guard.
    /// Use case: the persistence path must link clean under Native AOT / trimming, so it
    /// serializes ONLY through the source-generated context and never a reflective
    /// generic <c>JsonSerializer</c> overload.
    /// Acceptance: the generated <see cref="BindingJsonContext.Default"/> typeinfos exist
    /// (the generator ran), the library source references the <c>JsonTypeInfo</c> path,
    /// and no <c>ViceSharp.Xbox.Input</c> source file calls a reflective generic
    /// <c>JsonSerializer.Serialize&lt;&gt;</c>/<c>Deserialize&lt;&gt;</c> overload.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Serialization_GoesThroughSourceGenContext_NoReflectiveOverload()
    {
        // The source generator produced the metadata: these are non-null only if the
        // partial JsonSerializerContext was source-generated.
        Assert.NotNull(BindingJsonContext.Default);
        Assert.NotNull(BindingJsonContext.Default.BindingProfile);
        Assert.NotNull(BindingJsonContext.Default.ButtonBinding);

        var inputDir = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox.Input");
        Assert.True(Directory.Exists(inputDir), $"Expected the input library at '{inputDir}'.");

        // A reflective generic overload carries a type-argument list
        // (JsonSerializer.Serialize<SomeType>(...)); the AOT-safe overloads pass a
        // JsonTypeInfo positionally and have no '<...>'. Requiring an identifier after
        // '<' keeps an XML-doc close tag like "JsonSerializer.Serialize</c>" from
        // reading as a hit.
        var reflective = new Regex(
            @"JsonSerializer\s*\.\s*(Serialize|Deserialize)\s*<\s*[A-Za-z_]", RegexOptions.Compiled);

        var violations = new List<string>();
        bool usesContext = false;
        foreach (var file in Directory.EnumerateFiles(inputDir, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(file))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (reflective.IsMatch(text))
            {
                violations.Add(Path.GetRelativePath(RepoRoot, file));
            }

            if (text.Contains("BindingJsonContext.Default.BindingProfile", StringComparison.Ordinal))
            {
                usesContext = true;
            }
        }

        Assert.True(
            violations.Count == 0,
            "Reflective generic JsonSerializer overload found in ViceSharp.Xbox.Input:\n  " +
            string.Join("\n  ", violations));
        Assert.True(usesContext, "Expected the store to serialize via BindingJsonContext.Default.BindingProfile.");
    }

    /// <summary>
    /// FR-GAMEPAD-006 / FR-SYSBTN-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-012),
    /// TEST-SYSBTN-003 single-config-resolution guard.
    /// Use case: the converter and mapper must read deadzone and swap from ONE resolved
    /// <see cref="XboxInputConfig"/>, not from two competing stores.
    /// Acceptance: <see cref="XboxInputConfigResolver.Resolve"/> yields one config whose
    /// StickDeadzone and SwapPorts come from the prefs (thresholds from
    /// <see cref="XboxInputConfig.Default"/>); the SAME partial stick push is gated to
    /// Neutral under a high resolved deadzone and active under a low one (through both
    /// <see cref="StickConverter"/> and <see cref="XboxJoystickMapper"/>), and the
    /// resolved SwapPorts routes the primary bundle to the opposite port.
    /// </summary>
    /// <remarks>
    /// The two deadzones BRACKET the push rather than using 0.30/0.05: with the LOCKED
    /// <c>ActivateThreshold = 0.55</c>, any push weak enough to be gated by a 0.30
    /// deadzone (magnitude &lt; 0.30) can never reach 0.55 to set a direction bit, so a
    /// deadzone difference is only observable when the deadzones straddle an
    /// activatable push (here magnitude 0.62, gated by 0.70, active under 0.05). This
    /// preserves the spirit - the resolved deadzone genuinely flows into the converter -
    /// while respecting the frozen S8/S9 thresholds.
    /// </remarks>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Resolve_ProducesOneConfig_DeadzoneAndSwapFlowIntoConverterAndMapper()
    {
        const string profilePath = "bindings.v1.json";
        const double pushX = 0.62; // partial rightward push; magnitude 0.62.

        // High deadzone (0.70) gates the push; low deadzone (0.05) passes it.
        var highPrefs = new XboxInputPrefs(0.70, 0.70, SwapPorts: false, profilePath);
        var lowPrefs = new XboxInputPrefs(0.05, 0.05, SwapPorts: false, profilePath);

        XboxInputConfig high = XboxInputConfigResolver.Resolve(highPrefs);
        XboxInputConfig low = XboxInputConfigResolver.Resolve(lowPrefs);

        // ONE resolved config: deadzone + swap come from prefs, thresholds from Default.
        Assert.Equal(0.70, high.StickDeadzone);
        Assert.Equal(0.05, low.StickDeadzone);
        Assert.False(high.SwapPorts);
        Assert.False(low.SwapPorts);
        Assert.Equal(XboxInputConfig.Default.DiagonalThreshold, low.DiagonalThreshold);
        Assert.Equal(XboxInputConfig.Default.ActivateThreshold, low.ActivateThreshold);
        Assert.Equal(XboxInputConfig.Default.ReleaseThreshold, low.ReleaseThreshold);

        // The resolved deadzone flows into the converter: gated -> Neutral, else active.
        Assert.Equal((byte)0, StickConverter.ToDirectionMask(pushX, 0.0, 0, high));
        Assert.Equal(JoystickPortState.Right, StickConverter.ToDirectionMask(pushX, 0.0, 0, low));

        // And into the mapper (no swap -> primary/left-stick bundle drives JOY2).
        var reading = new GamepadSnapshot(pushX, 0.0, 0.0, 0.0, 0.0, 0.0, GamepadButtonFlags.None, 0UL);
        var highMap = XboxJoystickMapper.Map(reading, high, MapperState.Initial);
        var lowMap = XboxJoystickMapper.Map(reading, low, MapperState.Initial);
        Assert.Equal((byte)0, highMap.Joy2.DirectionMask);
        Assert.Equal(JoystickPortState.Right, lowMap.Joy2.DirectionMask);

        // The resolved SwapPorts flows into the mapper: primary bundle now drives JOY1.
        var swapPrefs = new XboxInputPrefs(0.05, 0.05, SwapPorts: true, profilePath);
        XboxInputConfig swap = XboxInputConfigResolver.Resolve(swapPrefs);
        Assert.True(swap.SwapPorts);
        var swapMap = XboxJoystickMapper.Map(reading, swap, MapperState.Initial);
        Assert.Equal(JoystickPortState.Right, swapMap.Joy1.DirectionMask);
        Assert.Equal((byte)0, swapMap.Joy2.DirectionMask);
    }

    private static bool IsBuildArtifact(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
            {
                directory = directory.Parent;
            }

            if (directory is null)
            {
                throw new InvalidOperationException("Could not locate repository root (ViceSharp.slnx).");
            }

            return directory.FullName;
        }
    }
}
