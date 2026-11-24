using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using ViceSharp.Build.Testing;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>
    /// Runs the host integration checks for TR-HOST-AFFINITY-001, TR-AUDIO-PACE-001,
    /// FR-CFG-002, and TEST-MONITOR-DISASM-002. Selects only the current host's
    /// platform-specific cases and the portable snapshot check.
    /// </summary>
    Target HostIntegrationTest => _ => _
        .Description("Run ViceSharp host integration tests for the current OS and retain the TRX")
        .Executes(() =>
        {
            var project = RootDirectory / "tests" / "ViceSharp.Host.IntegrationTests" / "ViceSharp.Host.IntegrationTests.csproj";
            var results = RootDirectory / "TestResults" / "host-integration" / DateTime.Now.ToString("yyyyMMddTHHmmss");
            var filter = "Category=Integration";
            if (!OperatingSystem.IsWindows())
                filter += "&Host!=Windows";
            if (!OperatingSystem.IsLinux())
                filter += "&Host!=Linux";

            const string trxName = "ViceSharp.Host.IntegrationTests.trx";
            DotNetTest(s => s
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .SetResultsDirectory(results)
                .SetLoggers($"trx;LogFileName={trxName}")
                .SetFilter(filter));

            var document = System.Xml.Linq.XDocument.Load(results / trxName);
            var counters = document.Descendants().Single(x => x.Name.LocalName == "Counters");
            int total = int.Parse(counters.Attribute("total")!.Value);
            int passed = int.Parse(counters.Attribute("passed")!.Value);
            if (total == 0 || total != passed)
                throw new InvalidOperationException($"ViceSharp.Host.IntegrationTests: every selected test must pass ({passed}/{total}).");
        });

    /// <summary>
    /// PLAN-NUKETESTS-001 / TR-BUILD-001 ac-12: runs the Nuke build tests in build.tests/ (outside
    /// ViceSharp.slnx, so Test and CiTest never see them). Linux-host cases are excluded by filter on
    /// other hosts, never skipped. Fails when zero tests are selected or any selected test does not pass.
    /// </summary>
    Target BuildTest => _ => _
        .Description("Run the Nuke build tests (build.tests) with host selection; every selected test must pass")
        .Executes(() =>
        {
            var project = RootDirectory / "build.tests" / "ViceSharp.Build.Tests.csproj";
            var results = RootDirectory / "TestResults" / "build-tests" / DateTime.Now.ToString("yyyyMMddTHHmmss");
            string? hostFilter = BuildTestFilter.For(OperatingSystem.IsLinux());
            DotNetTest(s => s
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .SetResultsDirectory(results)
                .SetLoggers("trx;LogFileName=ViceSharp.Build.Tests.trx")
                .When(_ => hostFilter is not null, x => x.SetFilter(hostFilter)));
            var document = System.Xml.Linq.XDocument.Load(results / "ViceSharp.Build.Tests.trx");
            var counters = document.Descendants().Single(x => x.Name.LocalName == "Counters");
            int total = int.Parse(counters.Attribute("total")!.Value);
            int passed = int.Parse(counters.Attribute("passed")!.Value);
            Serilog.Log.Information(
                "BuildTest: selected {Total}, passed {Passed}, host filter {Filter} (Linux-host cases are pinned by LinuxHostTrait_MarksOnlyTheDeploymentContract)",
                total, passed, hostFilter ?? "none");
            if (total == 0 || total != passed)
                throw new InvalidOperationException($"ViceSharp.Build.Tests: every selected test must pass ({passed}/{total}).");
        });
}
