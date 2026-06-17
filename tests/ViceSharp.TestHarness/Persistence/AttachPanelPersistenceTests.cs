namespace ViceSharp.TestHarness.Persistence;

using FluentAssertions;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using Xunit;

/// <summary>
/// FR/TR: FR-UIPERSIST-001 / TEST-UIPERSIST-002.
/// The attach panel exposes the two save-on-exit toggles and can capture its
/// current Settings-tab values into a persistable snapshot.
/// </summary>
public sealed class AttachPanelPersistenceTests
{
    [Fact]
    public void Toggles_AreSettable()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());

        vm.SaveSettingsOnExit = true;
        vm.SaveTransientValuesOnExit = true;

        vm.SaveSettingsOnExit.Should().BeTrue();
        vm.SaveTransientValuesOnExit.Should().BeTrue();
    }

    [Fact]
    public void CapturePersistedSettings_ReflectsCurrentSelections()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient())
        {
            SelectedPalette = "Pepto",
            SelectedDisplayScale = "3x",
            SwapJoystickPorts = true,
        };

        var snapshot = vm.CapturePersistedSettings();

        snapshot.Palette.Should().Be("Pepto");
        snapshot.DisplayScale.Should().Be("3x");
        snapshot.SwapJoystickPorts.Should().BeTrue();
    }

    [Fact]
    public void CapturePersistedTransient_WithNoAttachments_IsEmpty()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());

        var transient = vm.CapturePersistedTransient();

        transient.Attachments.Should().BeEmpty();
    }
}
