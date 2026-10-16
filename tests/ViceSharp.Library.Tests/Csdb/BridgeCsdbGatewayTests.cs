using System.Net.Http;
using FluentAssertions;
using RomM.Client;
using RomM.Client.Models;
using ViceSharp.Library.Tests.Adapter;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Csdb;

/// <summary>
/// FR-CSDB-001 (AC-CSDB-04). Use case: the bridge gateway POSTs the ingest to the sidecar and then
/// triggers the RomM scan itself (the bridge does not scan).
/// </summary>
[Trait("Category", "Library")]
public sealed class BridgeCsdbGatewayTests
{
    /// <summary>AC-CSDB-04: ingest POSTs /csdb/v1/ingest then calls ScanLibraryAsync.</summary>
    [Fact]
    [Trait("AC", "AC-CSDB-04")]
    public async Task Posts_ThenScans()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            req.RequestUri!.AbsolutePath == "/csdb/v1/ingest"
                ? FakeRomMHandler.Json("""{"requested":2,"items":[{"status":"ok"},{"status":"ok"}]}""")
                : FakeRomMHandler.NotFound();

        var handler = new FakeRomMHandler(Router);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8090/") };
        var tasks = new FakeTasksClient();
        var gateway = new BridgeCsdbGateway(http, tasks);

        CsdbIngestResult result = await gateway.IngestAndScanAsync(
            new[] { new CsdbSelection(101, CsdbKind.Demo), new CsdbSelection(102, CsdbKind.Sid) },
            force: false,
            ct);

        FakeRomMHandler.Captured post = handler.Requests.Single(r =>
            r.Method == HttpMethod.Post && r.Uri.AbsolutePath == "/csdb/v1/ingest");
        post.Body.Should().Contain("csdbId").And.Contain("101").And.Contain("demo");

        tasks.ScanCalls.Should().Be(1);
        result.Scanned.Should().BeTrue();
        result.Ingested.Should().Be(2);
    }
}

/// <summary>A RomM tasks client that records scan calls.</summary>
internal sealed class FakeTasksClient : IRomMTasksClient
{
    public int ScanCalls { get; private set; }

    public Task<IReadOnlyList<TaskInfo>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<TaskExecutionResponse> RunAsync(string taskName, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<TaskStatusResponse> GetStatusAsync(string taskId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task WaitAsync(string taskId, TimeSpan pollInterval, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task ScanLibraryAsync(CancellationToken cancellationToken = default)
    {
        ScanCalls++;
        return Task.CompletedTask;
    }
}
