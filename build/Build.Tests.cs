using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using ViceSharp.Build.Testing;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
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
