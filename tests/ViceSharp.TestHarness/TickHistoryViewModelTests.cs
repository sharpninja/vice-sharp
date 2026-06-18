namespace ViceSharp.TestHarness;

using System.Threading;
using NSubstitute;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-TICKHIST-001 / TR-TICKHIST-UI-001 / TEST-TICKHIST-001.
/// The History panel view-model: refreshes the last ticks (newest first) and, only when the
/// emulator is paused, opens a debug screen with the tick's registers and a reconstructed
/// memory dump.
/// </summary>
public sealed class TickHistoryViewModelTests
{
    private static GetTickHistoryResponse TwoTicks()
        => new(RpcStatus.Ok(), new[]
        {
            new TickHistoryEntryDto(0, 0x1000, 0xA9, 0x42, 0, 0, 0xFF, 0x20, 0x1002, 0),
            new TickHistoryEntryDto(1, 0x1002, 0x8D, 0x42, 0, 0, 0xFF, 0x20, 0x1005, 1),
        });

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: refreshing loads the captured ticks newest-first for the list.
    /// Acceptance: RefreshAsync populates Ticks in reverse index order (newest at top).
    /// </summary>
    [Fact]
    public async Task RefreshAsync_PopulatesTicksNewestFirst()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.GetTickHistoryAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetTickHistoryResponse>(TwoTicks()));
        var vm = new TickHistoryViewModel(host);

        await vm.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, vm.Ticks.Count);
        Assert.Equal(1, vm.Ticks[0].Index);
        Assert.Equal(0, vm.Ticks[1].Index);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: clicking a tick while RUNNING must not open the debug screen (inspection is
    ///   only meaningful when paused).
    /// Acceptance: InspectAsync with IsPaused=false leaves IsDebugVisible false and reads no
    ///   memory.
    /// </summary>
    [Fact]
    public async Task InspectAsync_WhenNotPaused_DoesNotOpenDebugScreen()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.GetTickHistoryAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetTickHistoryResponse>(TwoTicks()));
        var vm = new TickHistoryViewModel(host) { IsPaused = false };
        await vm.RefreshAsync(TestContext.Current.CancellationToken);

        await vm.InspectAsync(vm.Ticks[0], TestContext.Current.CancellationToken);

        Assert.False(vm.IsDebugVisible);
        Assert.Empty(vm.MemoryDump);
        await host.DidNotReceive().ReadMemoryAtTickAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: clicking a tick while PAUSED opens the debug screen with that tick's
    ///   registers and a reconstructed memory dump.
    /// Acceptance: InspectAsync with IsPaused=true sets IsDebugVisible, fills RegistersText
    ///   from the tick, and populates MemoryDump from ReadMemoryAtTick.
    /// </summary>
    [Fact]
    public async Task InspectAsync_WhenPaused_OpensDebugScreenWithMemoryDump()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.GetTickHistoryAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetTickHistoryResponse>(TwoTicks()));
        host.ReadMemoryAtTickAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MonitorMemoryResponse>(
                new MonitorMemoryResponse(RpcStatus.Ok(), 0, new byte[64], null)));
        host.GetChipStateAtTickAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetChipStateAtTickResponse>(
                new GetChipStateAtTickResponse(RpcStatus.Ok(), System.Array.Empty<ChipStateDto>())));
        var vm = new TickHistoryViewModel(host) { IsPaused = true };
        await vm.RefreshAsync(TestContext.Current.CancellationToken);

        await vm.InspectAsync(vm.Ticks[0], TestContext.Current.CancellationToken);

        Assert.True(vm.IsDebugVisible);
        Assert.NotEmpty(vm.MemoryDump);
        Assert.Contains("PC $", vm.RegistersText);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: returning from the debug screen clears it.
    /// Acceptance: CloseDebug sets IsDebugVisible false and empties the dump.
    /// </summary>
    [Fact]
    public async Task CloseDebug_ReturnsToList()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.GetTickHistoryAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetTickHistoryResponse>(TwoTicks()));
        host.ReadMemoryAtTickAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MonitorMemoryResponse>(
                new MonitorMemoryResponse(RpcStatus.Ok(), 0, new byte[64], null)));
        host.GetChipStateAtTickAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetChipStateAtTickResponse>(
                new GetChipStateAtTickResponse(RpcStatus.Ok(), System.Array.Empty<ChipStateDto>())));
        var vm = new TickHistoryViewModel(host) { IsPaused = true };
        await vm.RefreshAsync(TestContext.Current.CancellationToken);
        await vm.InspectAsync(vm.Ticks[0], TestContext.Current.CancellationToken);

        vm.CloseDebug();

        Assert.False(vm.IsDebugVisible);
        Assert.Empty(vm.MemoryDump);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: the hex dump renders classic address/hex/ascii lines.
    /// Acceptance: HexDump.Format yields one line per 16 bytes, starting with the address.
    /// </summary>
    [Fact]
    public void HexDump_Format_RendersAddressedLines()
    {
        var data = new byte[32];
        data[0] = 0x41; // 'A'

        var lines = new System.Collections.Generic.List<string>(HexDump.Format(data, baseAddress: 0x0400));

        Assert.Equal(2, lines.Count);
        Assert.StartsWith("0400", lines[0]);
        Assert.StartsWith("0410", lines[1]);
        Assert.Contains("41", lines[0]);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: the memory search query is parsed per the selected interpretation (hex,
    ///   decimal, or a string encoding) into a byte pattern.
    /// Acceptance: each (kind, query) parses to the expected bytes.
    /// </summary>
    [Theory]
    [InlineData("Hex", "DEAD", new byte[] { 0xDE, 0xAD })]
    [InlineData("Hex", "DE AD", new byte[] { 0xDE, 0xAD })]
    [InlineData("Decimal", "65 66", new byte[] { 65, 66 })]
    [InlineData("ASCII", "AB", new byte[] { 0x41, 0x42 })]
    [InlineData("PETSCII", "ab", new byte[] { 0x41, 0x42 })]
    [InlineData("Screen codes", "AB", new byte[] { 0x01, 0x02 })]
    public void MemorySearch_ParsesPatternPerKind(string kind, string query, byte[] expected)
    {
        Assert.True(MemorySearch.TryParsePattern(query, kind, out var pattern));
        Assert.Equal(expected, pattern);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: locating a byte pattern in a memory image.
    /// Acceptance: IndexOf returns the first offset of a present pattern and -1 when absent.
    /// </summary>
    [Fact]
    public void MemorySearch_IndexOf_FindsFirstMatchElseMinusOne()
    {
        var image = new byte[64];
        image[0x12] = 0xDE;
        image[0x13] = 0xAD;

        Assert.Equal(0x12, MemorySearch.IndexOf(image, new byte[] { 0xDE, 0xAD }));
        Assert.Equal(-1, MemorySearch.IndexOf(image, new byte[] { 0xBE, 0xEF }));
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: searching the reconstructed memory of an inspected tick scrolls the dump to
    ///   the matching line.
    /// Acceptance: a hex search for a present pattern sets MatchLineIndex to offset/16 and
    ///   reports the address; an absent pattern reports not-found with MatchLineIndex -1.
    /// </summary>
    [Fact]
    public async Task SearchMemory_AfterInspect_SetsMatchLineForPresentPattern()
    {
        var data = new byte[256];
        data[0x30] = 0xDE;
        data[0x31] = 0xAD;
        var host = Substitute.For<IHostProtocolClient>();
        host.GetTickHistoryAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetTickHistoryResponse>(TwoTicks()));
        host.ReadMemoryAtTickAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MonitorMemoryResponse>(new MonitorMemoryResponse(RpcStatus.Ok(), 0, data, null)));
        host.GetChipStateAtTickAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetChipStateAtTickResponse>(
                new GetChipStateAtTickResponse(RpcStatus.Ok(), System.Array.Empty<ChipStateDto>())));
        var vm = new TickHistoryViewModel(host) { IsPaused = true };
        await vm.RefreshAsync(TestContext.Current.CancellationToken);
        await vm.InspectAsync(vm.Ticks[0], TestContext.Current.CancellationToken);

        vm.SelectedSearchKind = "Hex";
        vm.SearchQuery = "DEAD";
        vm.SearchMemory();

        Assert.Equal(0x30 / 16, vm.MatchLineIndex);
        Assert.Contains("0030", vm.SearchStatus);

        vm.SearchQuery = "BEEF";
        vm.SearchMemory();
        Assert.Equal(-1, vm.MatchLineIndex);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-UI-001.
    /// Use case: First / Back / Next / Last buttons step the debug screen through the
    ///   captured ticks (clamped at the ends), updating the inspected tick.
    /// Acceptance: NavigateFirst/Last jump to index 0 / the newest index; Previous/Next move
    ///   by one and clamp; CanNavigatePrevious/Next reflect the position.
    /// </summary>
    [Fact]
    public async Task Navigate_StepsThroughTicks_AndClampsAtEnds()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.GetTickHistoryAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetTickHistoryResponse>(TwoTicks()));
        host.ReadMemoryAtTickAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MonitorMemoryResponse>(new MonitorMemoryResponse(RpcStatus.Ok(), 0, new byte[64], null)));
        host.GetChipStateAtTickAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetChipStateAtTickResponse>(
                new GetChipStateAtTickResponse(RpcStatus.Ok(), System.Array.Empty<ChipStateDto>())));
        var vm = new TickHistoryViewModel(host) { IsPaused = true };
        await vm.RefreshAsync(TestContext.Current.CancellationToken);
        await vm.InspectAsync(vm.Ticks[0], TestContext.Current.CancellationToken); // newest (index 1)

        Assert.Equal(1, vm.SelectedTick!.Index);
        Assert.True(vm.CanNavigatePrevious);
        Assert.False(vm.CanNavigateNext);

        await vm.NavigateFirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, vm.SelectedTick!.Index);
        Assert.False(vm.CanNavigatePrevious);
        Assert.True(vm.CanNavigateNext);

        await vm.NavigateNextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, vm.SelectedTick!.Index);

        await vm.NavigatePreviousAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, vm.SelectedTick!.Index);

        // Clamp: Back at the first tick stays at index 0.
        await vm.NavigatePreviousAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, vm.SelectedTick!.Index);

        await vm.NavigateLastAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, vm.SelectedTick!.Index);
    }
}
