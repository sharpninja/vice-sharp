namespace ViceSharp.TestHarness.Xbox;

using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S15 (IMPL-XBOXUWP-015). FR-XSET-001 / TR-XSET-002,
/// TEST-XSET-001. Guards that the portable, AOT/trim-safe
/// <see cref="SettingsOptionCatalog"/> in <c>ViceSharp.Protocol</c> single-sources
/// the emulator settings option lists and their label &lt;-&gt; stored-id maps
/// byte-identical to the desktop tables in
/// <see cref="AttachPanelViewModel"/>, so the Xbox 10-foot settings UI and the
/// desktop attach panel share exactly one definition and cannot drift.
/// </summary>
public sealed class SettingsOptionCatalogTests
{
    private static AttachPanelViewModel CreateDesktopViewModel() =>
        new(new DisconnectedHostProtocolClient());

    // ---- Parity: catalog lists equal the desktop AttachPanelViewModel tables ----

    /// <summary>
    /// FR: FR-XSET-001, TR: TR-XSET-002 (IMPL-XBOXUWP-015), TEST-XSET-001.
    /// Use case: the Xbox settings UI must present exactly the same option
    /// labels, in the same order, as the desktop attach panel, so both surfaces
    /// share one definition and cannot drift apart.
    /// Acceptance: every <see cref="SettingsOptionCatalog"/> option list equals
    /// the corresponding <see cref="AttachPanelViewModel"/> list element-for-element.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void CatalogLists_MatchDesktopViewModel_ElementForElement()
    {
        var vm = CreateDesktopViewModel();

        Assert.Equal(vm.RendererModes, SettingsOptionCatalog.RendererModes);
        Assert.Equal(vm.DisplayScales, SettingsOptionCatalog.DisplayScales);
        Assert.Equal(vm.CropModes, SettingsOptionCatalog.CropModes);
        Assert.Equal(vm.AspectModes, SettingsOptionCatalog.AspectModes);
        Assert.Equal(vm.PaletteModes, SettingsOptionCatalog.PaletteModes);
        Assert.Equal(vm.AudioModes, SettingsOptionCatalog.AudioModes);
        Assert.Equal(vm.InputModes, SettingsOptionCatalog.InputModes);
        Assert.Equal(vm.PrimaryJoystickPorts, SettingsOptionCatalog.PrimaryJoystickPorts);
        Assert.Equal(vm.ResourceModes, SettingsOptionCatalog.ResourceModes);
        Assert.Equal(vm.PacingStrategies, SettingsOptionCatalog.PacingStrategies);
    }

    /// <summary>
    /// FR: FR-XSET-001, TR: TR-XSET-002 (IMPL-XBOXUWP-015), TEST-XSET-001.
    /// Use case: the plan fixes the catalog at exactly ten option lists; a
    /// dropped or added list would silently desync one settings surface.
    /// Acceptance: the ten catalog lists are each non-empty with the exact
    /// desktop element counts.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void CatalogLists_HaveExpectedShape()
    {
        Assert.Equal(2, SettingsOptionCatalog.RendererModes.Count);
        Assert.Equal(4, SettingsOptionCatalog.DisplayScales.Count);
        Assert.Equal(3, SettingsOptionCatalog.CropModes.Count);
        Assert.Equal(3, SettingsOptionCatalog.AspectModes.Count);
        Assert.Equal(4, SettingsOptionCatalog.PaletteModes.Count);
        Assert.Equal(3, SettingsOptionCatalog.AudioModes.Count);
        Assert.Equal(3, SettingsOptionCatalog.InputModes.Count);
        Assert.Equal(2, SettingsOptionCatalog.PrimaryJoystickPorts.Count);
        Assert.Equal(3, SettingsOptionCatalog.ResourceModes.Count);
        Assert.Equal(2, SettingsOptionCatalog.PacingStrategies.Count);
    }

    // ---- Round-trip stability: From*(To*(label)) == label for every option ----

    /// <summary>
    /// FR: FR-XSET-001, TR: TR-XSET-002 (IMPL-XBOXUWP-015), TEST-XSET-001.
    /// Use case: the settings gateway maps a display label to a stored id when
    /// applying and back to a label when adopting host-canonical settings; that
    /// path must be lossless for every selectable option.
    /// Acceptance: for every label in every catalog list,
    /// <c>From*(To*(label)) == label</c> (bool-parameterised From* maps are
    /// exercised with both argument values since every real label round-trips
    /// through an explicit id, independent of the fallback flag).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ToFrom_RoundTripsEveryOption()
    {
        foreach (var renderer in SettingsOptionCatalog.RendererModes)
            Assert.Equal(renderer, SettingsOptionCatalog.FromRendererId(SettingsOptionCatalog.ToRendererId(renderer)));

        foreach (var scale in SettingsOptionCatalog.DisplayScales)
            Assert.Equal(scale, SettingsOptionCatalog.FromScaleId(SettingsOptionCatalog.ToScaleId(scale)));

        foreach (var crop in SettingsOptionCatalog.CropModes)
        {
            Assert.Equal(crop, SettingsOptionCatalog.FromCropModeId(SettingsOptionCatalog.ToCropModeId(crop), showBorder: true));
            Assert.Equal(crop, SettingsOptionCatalog.FromCropModeId(SettingsOptionCatalog.ToCropModeId(crop), showBorder: false));
        }

        foreach (var aspect in SettingsOptionCatalog.AspectModes)
        {
            Assert.Equal(aspect, SettingsOptionCatalog.FromAspectModeId(SettingsOptionCatalog.ToAspectModeId(aspect), maintainAspectRatio: true));
            Assert.Equal(aspect, SettingsOptionCatalog.FromAspectModeId(SettingsOptionCatalog.ToAspectModeId(aspect), maintainAspectRatio: false));
        }

        foreach (var palette in SettingsOptionCatalog.PaletteModes)
            Assert.Equal(palette, SettingsOptionCatalog.FromPaletteId(SettingsOptionCatalog.ToPaletteId(palette)));

        foreach (var audio in SettingsOptionCatalog.AudioModes)
            Assert.Equal(audio, SettingsOptionCatalog.FromAudioModeId(SettingsOptionCatalog.ToAudioModeId(audio)));

        foreach (var input in SettingsOptionCatalog.InputModes)
            Assert.Equal(input, SettingsOptionCatalog.FromInputModeId(SettingsOptionCatalog.ToInputModeId(input)));

        foreach (var port in SettingsOptionCatalog.PrimaryJoystickPorts)
            Assert.Equal(port, SettingsOptionCatalog.FromInputPort(SettingsOptionCatalog.ToInputPort(port)));

        foreach (var resource in SettingsOptionCatalog.ResourceModes)
            Assert.Equal(resource, SettingsOptionCatalog.FromResourceModeId(SettingsOptionCatalog.ToResourceModeId(resource)));

        foreach (var pacing in SettingsOptionCatalog.PacingStrategies)
            Assert.Equal(pacing, SettingsOptionCatalog.FromPacingStrategyId(SettingsOptionCatalog.ToPacingStrategyId(pacing)));
    }

    // ---- Anchor values: the exact desktop ids/labels the plan pins ----

    /// <summary>
    /// FR: FR-XSET-001, TR: TR-XSET-002 (IMPL-XBOXUWP-015), TEST-XSET-001.
    /// Use case: the DTO ids the host receives must be the exact desktop stored
    /// ids; a renamed id would silently change host behaviour for one surface.
    /// Acceptance: the anchor conversions match the desktop tables exactly:
    /// <c>ToPaletteId("Pepto")=="pepto"</c>, <c>ToCropModeId("Borderless")=="borderless"</c>,
    /// and <c>ToInputPort("Joystick 1")==InputPort.Joystick1</c>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void AnchorConversions_MatchDesktopIds()
    {
        Assert.Equal("pepto", SettingsOptionCatalog.ToPaletteId("Pepto"));
        Assert.Equal("borderless", SettingsOptionCatalog.ToCropModeId("Borderless"));
        Assert.Equal(InputPort.Joystick1, SettingsOptionCatalog.ToInputPort("Joystick 1"));
    }

    /// <summary>
    /// FR: FR-XSET-001, TR: TR-XSET-002 (IMPL-XBOXUWP-015), TEST-XSET-001.
    /// Use case: additional per-map anchors lock the default (fallback) branch
    /// of each conversion so a reordered switch cannot silently change the
    /// canonical id for the first/default option.
    /// Acceptance: the default-branch ids match the desktop tables exactly.
    /// </summary>
    [Theory]
    [Trait("Category", "Xbox")]
    [InlineData("Host direct", "host")]
    [InlineData("Software", "software")]
    [InlineData("Fit window", "fit-window")]
    [InlineData("2x", "2x")]
    public void ToRendererAndScaleIds_MatchDesktop(string label, string expectedId)
    {
        var actual = label switch
        {
            "Host direct" or "Software" => SettingsOptionCatalog.ToRendererId(label),
            _ => SettingsOptionCatalog.ToScaleId(label)
        };

        Assert.Equal(expectedId, actual);
    }

    /// <summary>
    /// FR: FR-XSET-001, TR: TR-XSET-002 (IMPL-XBOXUWP-015), TEST-XSET-001.
    /// Use case: the aspect / audio / input / resource / pacing maps each have a
    /// default fallback id; these must equal the desktop defaults so the two
    /// surfaces agree on the canonical value.
    /// Acceptance: each To* default matches the desktop table.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ToIds_DefaultBranches_MatchDesktop()
    {
        Assert.Equal("vice", SettingsOptionCatalog.ToPaletteId("VICE default"));
        Assert.Equal("visible-area", SettingsOptionCatalog.ToCropModeId("Visible area"));
        Assert.Equal("vice-pixel-aspect", SettingsOptionCatalog.ToAspectModeId("VICE pixel aspect"));
        Assert.Equal("enabled", SettingsOptionCatalog.ToAudioModeId("Enabled"));
        Assert.Equal("keyboard-joystick", SettingsOptionCatalog.ToInputModeId("Keyboard + joystick"));
        Assert.Equal("auto-detect", SettingsOptionCatalog.ToResourceModeId("Auto detect"));
        Assert.Equal("semaphore", SettingsOptionCatalog.ToPacingStrategyId("Semaphore"));
        Assert.Equal("vice", SettingsOptionCatalog.ToPacingStrategyId("VICE"));
        Assert.Equal(InputPort.Joystick2, SettingsOptionCatalog.ToInputPort("Joystick 2"));
    }
}
