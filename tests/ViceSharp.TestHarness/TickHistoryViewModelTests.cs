namespace ViceSharp.TestHarness;

using System.Threading;
using NSubstitute;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-TICKHIST-001 / TR-TICKHIST-006 / TEST-TICKHIST-003.
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
    /// FR-TICKHIST-001 / TR-TICKHIST-006.
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
    /// FR-TICKHIST-001 / TR-TICKHIST-006.
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
    /// FR-TICKHIST-001 / TR-TICKHIST-003.
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
    /// FR-TICKHIST-001 / TR-TICKHIST-006.
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
    /// FR-TICKHIST-001 / TR-TICKHIST-003.
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
}
