using FluentAssertions;
using ViceSharp.AdhocHelper;
using Xunit;

namespace ViceSharp.TestHarness.AdhocHelper;

/// <summary>
/// FR: FR-ARCH-ADHOC, TR: TR-ARCH-ADHOC-HELPER-VM.
/// Tests for the headless view-model that drives the ad-hoc machine YAML
/// authoring helper. The view-model wraps <see cref="ViceSharp.Architectures.Adhoc.AdhocMachineYamlLoader"/>
/// so it can be exercised without spinning up the Avalonia view.
/// </summary>
public sealed class AdhocHelperViewModelTests
{
    private const string ValidMinimalYaml = """
        schemaVersion: 1
        machine:
          name: "Tiny"
          videoStandard: Ntsc
          masterClockHz: 1000000
        memory:
          regions:
            - id: ram-main
              kind: Ram
              start: 0x0000
              end:   0xFFFF
        chips:
          - id: cpu
            type: Mos6502
            role: Cpu
        """;

    /// <summary>
    /// FR: FR-ARCH-ADHOC, TR: TR-ARCH-ADHOC-HELPER-VM.
    /// Use case: User opens the helper, leaves the editor blank, and clicks Validate.
    /// Acceptance: The validation message names a schema field (either the
    /// required-field path or the empty-yaml-document message) so the user
    /// knows what to fix.
    /// </summary>
    [Fact]
    public void Validate_EmptyYaml_ReportsRequiredFieldOrEmptyDocument()
    {
        var vm = new AdhocHelperViewModel
        {
            YamlText = string.Empty,
        };

        vm.Validate();

        vm.ValidationMessage.Should().NotBeNullOrWhiteSpace();
        var message = vm.ValidationMessage!.ToLowerInvariant();
        var validPrefix = message.StartsWith("required field")
                       || message.StartsWith("yaml document")
                       || message.StartsWith("yaml parse");
        validPrefix.Should().BeTrue(
            "empty yaml should report a required-field error or an empty-document error, but was: " + vm.ValidationMessage);
    }

    /// <summary>
    /// FR: FR-ARCH-ADHOC, TR: TR-ARCH-ADHOC-HELPER-VM.
    /// Use case: User pastes a minimal valid ad-hoc machine YAML and clicks
    /// Validate.
    /// Acceptance: Validation message starts with "OK" and contains the
    /// declared machine name, chip count, and region count so the user can
    /// confirm the loader interpreted the document correctly.
    /// </summary>
    [Fact]
    public void Validate_ValidYaml_ReportsOkWithSummary()
    {
        var vm = new AdhocHelperViewModel
        {
            YamlText = ValidMinimalYaml,
        };

        vm.Validate();

        vm.ValidationMessage.Should().NotBeNullOrWhiteSpace();
        vm.ValidationMessage!.Should().StartWith("OK");
        vm.ValidationMessage.Should().Contain("Tiny");
        vm.ValidationMessage.Should().Contain("1 chips");
        vm.ValidationMessage.Should().Contain("1 regions");
    }

    /// <summary>
    /// FR: FR-ARCH-ADHOC, TR: TR-ARCH-ADHOC-HELPER-VM.
    /// Use case: User pastes a YAML document whose schemaVersion the loader
    /// does not support (e.g. a future schema version).
    /// Acceptance: Validation message echoes the loader's "is not supported"
    /// language so the user can downgrade the document or upgrade the loader.
    /// </summary>
    [Fact]
    public void Validate_UnsupportedSchemaVersion_ReportsNotSupported()
    {
        const string yaml = """
            schemaVersion: 99
            machine:
              name: "Future"
              videoStandard: Pal
              masterClockHz: 1
            memory:
              regions:
                - id: ram
                  kind: Ram
                  start: 0
                  end: 0xFFFF
            chips:
              - id: cpu
                type: Mos6502
            """;

        var vm = new AdhocHelperViewModel
        {
            YamlText = yaml,
        };

        vm.Validate();

        vm.ValidationMessage.Should().NotBeNullOrWhiteSpace();
        vm.ValidationMessage!.Should().Contain("is not supported");
    }
}
