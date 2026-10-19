using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Run the RomM desktop contract mock gate before implementation")]
    readonly bool RommContractMocks;

    Target RommDesktopTest => _ => _
        .Description("Build and test managed RomM desktop/library projects without the native harness")
        .Executes(() =>
        {
            var previous = Environment.GetEnvironmentVariable("VICESHARP_ROMM_CONTRACT_MOCKS");
            try
            {
                Environment.SetEnvironmentVariable("VICESHARP_ROMM_CONTRACT_MOCKS", RommContractMocks ? "1" : null);
                var projects = RommContractMocks
                    ? new[] { "ViceSharp.Heads.Tests" }
                    : new[] { "ViceSharp.Heads.Tests", "ViceSharp.Library.Tests" };
                foreach (var project in projects)
                {
                    var results = RootDirectory / "TestResults" / "romm-desktop" /
                        $"{DateTime.Now:yyyyMMddTHHmmss}-{(RommContractMocks ? "mocks" : "real")}";
                    DotNetTest(s => s
                        .SetProjectFile(RootDirectory / "tests" / project / $"{project}.csproj")
                        .SetConfiguration(Configuration)
                        .SetResultsDirectory(results)
                        .SetLoggers($"trx;LogFileName={project}.trx")
                        // PLAN-NUKETESTS-001: the Linux-only deployment cases moved to build.tests
                        // (target BuildTest), so the Library tests need no host filter here.
                        .When(_ => RommContractMocks, x => x.SetFilter("FullyQualifiedName~RommDesktopContractTests")));
                    var document = System.Xml.Linq.XDocument.Load(results / $"{project}.trx");
                    var counters = document.Descendants().Single(x => x.Name.LocalName == "Counters");
                    int total = int.Parse(counters.Attribute("total")!.Value);
                    int passed = int.Parse(counters.Attribute("passed")!.Value);
                    if (total == 0 || total != passed)
                        throw new InvalidOperationException($"{project}: every selected test must pass ({passed}/{total}).");
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("VICESHARP_ROMM_CONTRACT_MOCKS", previous);
            }
        });
}
