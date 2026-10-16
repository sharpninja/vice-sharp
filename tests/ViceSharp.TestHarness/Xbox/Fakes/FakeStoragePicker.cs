namespace ViceSharp.TestHarness.Xbox.Fakes;

using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. Off-console test double for
/// <see cref="IStoragePicker"/>: it stands in for the head's UWP file picker and returns
/// a single canned <see cref="PickedFile"/> (or <c>null</c> to model a cancelled pick).
/// </summary>
internal sealed class FakeStoragePicker : IStoragePicker
{
    private readonly PickedFile? _file;

    /// <summary>Creates the fake picker.</summary>
    /// <param name="file">The file to return from <see cref="PickAsync"/>, or <c>null</c> for a cancelled pick.</param>
    public FakeStoragePicker(PickedFile? file)
    {
        _file = file;
    }

    /// <summary>Number of <see cref="PickAsync"/> calls received.</summary>
    public int PickCount { get; private set; }

    /// <inheritdoc />
    public Task<PickedFile?> PickAsync(CancellationToken ct = default)
    {
        PickCount++;
        return Task.FromResult(_file);
    }
}
