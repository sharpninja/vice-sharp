namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S1 (IMPL-XBOXUWP-001): extract the Kestrel-free POCO host
/// layer out of ViceSharp.Host into a new ViceSharp.Host.InProcess so the Xbox
/// UWP head can reuse it without dragging Grpc.AspNetCore/Kestrel into the
/// AppContainer.
///
/// FR-INPROC-001 / TR-INPROC-001 (Kestrel-free in-process host), TEST: this file.
/// Use case: the console head composes the emulator in-process (no gRPC server).
/// Acceptance:
///   - src/ViceSharp.Host.InProcess/ViceSharp.Host.InProcess.csproj exists,
///     targets net10.0, and carries NO Grpc.AspNetCore / Microsoft.AspNetCore
///     reference (the AppContainer-hostile Kestrel dependency).
///   - src/ViceSharp.Host/ViceSharp.Host.csproj references
///     ViceSharp.Host.InProcess and STILL keeps the Grpc.AspNetCore adapters.
///   - The POCO host types (EmulatorHostService, EmulationPumpService,
///     LocalVideoFrameSource, InputServiceHost, DefaultEmulatorRuntimeFactory)
///     now live in the ViceSharp.Host.InProcess assembly, with their namespaces
///     preserved.
///
/// RED before S1 lands: the csproj does not exist and the POCO types still
/// resolve to the ViceSharp.Host assembly.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class HostInProcessExtractionTests
{
    private const string HostInProcessAssembly = "ViceSharp.Host.InProcess";

    [Fact]
    public void HostInProcessCsproj_Exists_TargetsNet10_AndHasNoAspNetCore()
    {
        var csprojPath = Path.Combine(
            RepoRoot,
            "src",
            "ViceSharp.Host.InProcess",
            "ViceSharp.Host.InProcess.csproj");

        Assert.True(
            File.Exists(csprojPath),
            $"Expected the extracted in-process host csproj at '{csprojPath}'.");

        var project = File.ReadAllText(csprojPath);

        // TargetFramework is inherited from Directory.Build.props (net10.0); repo
        // libraries do not restate it. The invariant that matters is that this
        // project stays PORTABLE (no platform-pinned net10.0-windows TFM) and
        // carries no AppContainer-hostile Kestrel/gRPC-server dependency.
        Assert.DoesNotContain("-windows", project);
        Assert.DoesNotContain("Grpc.AspNetCore", project);
        Assert.DoesNotContain("Microsoft.AspNetCore", project);

        // The reused emulation core must be referenced directly.
        Assert.Contains("ViceSharp.Abstractions.csproj", project);
        Assert.Contains("ViceSharp.Core.csproj", project);
        Assert.Contains("ViceSharp.Chips.csproj", project);
        Assert.Contains("ViceSharp.Architectures.csproj", project);
    }

    [Fact]
    public void HostCsproj_ReferencesHostInProcess_AndKeepsGrpcAdapters()
    {
        var hostCsprojPath = Path.Combine(
            RepoRoot,
            "src",
            "ViceSharp.Host",
            "ViceSharp.Host.csproj");

        Assert.True(File.Exists(hostCsprojPath), $"Expected ViceSharp.Host csproj at '{hostCsprojPath}'.");

        var project = File.ReadAllText(hostCsprojPath);

        // The gRPC adapter layer stays in ViceSharp.Host on top of the extracted host.
        Assert.Contains("ViceSharp.Host.InProcess.csproj", project);
        Assert.Contains("Grpc.AspNetCore", project);
    }

    [Fact]
    public void PocoHostTypes_LiveInHostInProcessAssembly()
    {
        AssertAssembly(typeof(ViceSharp.Host.Services.EmulatorHostService));
        AssertAssembly(typeof(ViceSharp.Host.Services.EmulationPumpService));
        AssertAssembly(typeof(ViceSharp.Host.Services.LocalVideoFrameSource));
        AssertAssembly(typeof(ViceSharp.Host.Services.InputServiceHost));
        AssertAssembly(typeof(ViceSharp.Host.Runtime.DefaultEmulatorRuntimeFactory));
    }

    private static void AssertAssembly(Type type)
    {
        var actual = type.Assembly.GetName().Name;
        Assert.True(
            string.Equals(actual, HostInProcessAssembly, StringComparison.Ordinal),
            $"Expected '{type.FullName}' to live in '{HostInProcessAssembly}' after the S1 extraction, but it is in '{actual}'.");
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
