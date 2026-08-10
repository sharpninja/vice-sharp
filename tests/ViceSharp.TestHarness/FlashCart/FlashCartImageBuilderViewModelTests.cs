namespace ViceSharp.TestHarness.FlashCart;

using ViceSharp.Core.FlashCarts;
using Xunit;

/// <summary>
/// FR-FLASHCART-001 Slice B: portable builder ViewModel.
/// </summary>
public sealed class FlashCartImageBuilderViewModelTests
{
    [Fact]
    public void ChangingProfile_RebuildsBankList()
    {
        var vm = new FlashCartImageBuilderViewModel();
        Assert.Equal(64, vm.Banks.Count);
        vm.SelectedProfileId = "ultimem";
        Assert.Equal(FlashCartProfiles.Ultimem.BankCount, vm.Banks.Count);
        Assert.Contains("Ultimem", vm.ImageSizeSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportRaw_SetsSourceAndFilled()
    {
        var vm = new FlashCartImageBuilderViewModel();
        vm.TreatAsPrg = false;
        vm.SelectedBankIndex = 0;
        vm.ImportBank(new byte[] { 1, 2, 3 }, "chunk.bin");
        Assert.True(vm.Banks[0].IsFilled);
        Assert.Equal("chunk.bin", vm.Banks[0].SourceName);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public void Build_MatchesCoreBuilderBytes()
    {
        var vm = new FlashCartImageBuilderViewModel();
        vm.TreatAsPrg = false;
        var data = new byte[16];
        data[0] = 0x60;
        vm.ImportBank(data, "a.bin");
        var fromVm = vm.Build();

        var core = new FlashImageBuilder(FlashCartProfiles.Fe3);
        core.WriteRaw(0, data, sourceName: "a.bin");
        var fromCore = core.Build();
        Assert.Equal(fromCore, fromVm);
    }

    [Fact]
    public void ClearBank_RestoresEmpty()
    {
        var vm = new FlashCartImageBuilderViewModel();
        vm.TreatAsPrg = false;
        vm.ImportBank(new byte[] { 9, 9, 9 }, "x.bin");
        vm.ClearSelectedBank();
        Assert.False(vm.Banks[0].IsFilled);
        Assert.Equal(0xFF, vm.Build()[0]);
    }

    [Fact]
    public async Task SaveAsync_WritesViaFileIo()
    {
        var io = new FakeFileIo();
        var vm = new FlashCartImageBuilderViewModel(io);
        await vm.SaveAsync(@"C:\tmp\fe3.bin", TestContext.Current.CancellationToken);
        Assert.True(io.Writes.ContainsKey(@"C:\tmp\fe3.bin"));
        Assert.Equal(FlashCartProfiles.Fe3.ImageSizeBytes, io.Writes[@"C:\tmp\fe3.bin"].Length);
    }

    private sealed class FakeFileIo : IFlashBuilderFileIo
    {
        public Dictionary<string, byte[]> Writes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        {
            Writes[path] = data;
            return Task.CompletedTask;
        }
    }
}
