namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001/002, TR-XPATH-001, TR-MVVM-001.
/// The portable first-run ROM-provisioning ViewModel. It surfaces the current
/// <see cref="RomProvisionAssessment"/>, imports a picked ROM file (validated against the
/// 64&#160;MB ceiling + the target spec's size + MD5), and runs the confirm-gated verified
/// core-set download.
/// </summary>
/// <remarks>
/// Pure MVVM: it references only the portable contracts and the two host seams
/// (<see cref="IRomAcquirer"/>, <see cref="IStoragePicker"/>) plus <c>System.IO</c> for the
/// writable C64 directory. It never references RomFetch/Core (TR-MVVM-001); parity with those
/// is documented on <see cref="RomCatalog.C64"/> and the evaluator. The download is
/// confirm-gated (<see cref="ConfirmDownload"/> then <see cref="DownloadAsync"/>) so a network
/// fetch can never fire from a stray command.
/// </remarks>
public sealed class XboxRomProvisioningViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// The maximum size (bytes) an imported ROM file may report before it is rejected outright.
    /// 64&#160;MiB is far above any real Commodore ROM and guards against reading a huge file
    /// into memory (plan section 4f import-ceiling guard).
    /// </summary>
    public const long MaxImportBytes = 64L * 1024 * 1024;

    private readonly IRomAcquirer _acquirer;
    private readonly IStoragePicker _picker;
    private readonly RomProvisionEvaluator _evaluator;
    private readonly string _c64Directory;
    private readonly RomProfile _profile;

    private RomProvisionAssessment? _assessment;
    private string _statusMessage = string.Empty;
    private bool _isDownloadConfirmed;

    /// <summary>Creates the provisioning ViewModel.</summary>
    /// <param name="acquirer">The verified-download seam.</param>
    /// <param name="picker">The storage-import seam.</param>
    /// <param name="evaluator">The provisioning evaluator (also supplies the catalog for import validation).</param>
    /// <param name="c64Directory">The writable C64 ROM directory that imports/downloads land in and evaluation reads.</param>
    /// <param name="profile">The requirement profile (governs the Ultimax kernal-optional rule).</param>
    /// <exception cref="ArgumentNullException">A required argument is <c>null</c>.</exception>
    public XboxRomProvisioningViewModel(
        IRomAcquirer acquirer,
        IStoragePicker picker,
        RomProvisionEvaluator evaluator,
        string c64Directory,
        RomProfile profile)
    {
        _acquirer = acquirer ?? throw new ArgumentNullException(nameof(acquirer));
        _picker = picker ?? throw new ArgumentNullException(nameof(picker));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _c64Directory = c64Directory ?? throw new ArgumentNullException(nameof(c64Directory));
        _profile = profile;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The most recent provisioning assessment, or <c>null</c> before the first refresh.</summary>
    public RomProvisionAssessment? Assessment
    {
        get => _assessment;
        private set
        {
            _assessment = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Assessment)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBootBlocked)));
        }
    }

    /// <summary>The overall provisioning state (defaults to <see cref="RomProvisionState.NotProvisioned"/> before the first refresh).</summary>
    public RomProvisionState State => _assessment?.State ?? RomProvisionState.NotProvisioned;

    /// <summary>
    /// Whether normal boot is blocked. It is <c>true</c> until the assessment is
    /// <see cref="RomProvisionState.Complete"/> (accounting for the Ultimax kernal-optional rule),
    /// and defaults to <c>true</c> before the first refresh.
    /// </summary>
    public bool IsBootBlocked => _assessment?.IsBootBlocked ?? true;

    /// <summary>A short human-readable status message describing the last operation.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Whether the user has confirmed the verified download. <see cref="DownloadAsync"/> is a
    /// no-op until this is set via <see cref="ConfirmDownload"/>; it is reset after each download attempt.
    /// </summary>
    public bool IsDownloadConfirmed
    {
        get => _isDownloadConfirmed;
        private set => SetProperty(ref _isDownloadConfirmed, value);
    }

    /// <summary>Records the user's explicit confirmation to run the verified download.</summary>
    public void ConfirmDownload() => IsDownloadConfirmed = true;

    /// <summary>Clears a pending download confirmation.</summary>
    public void CancelDownload() => IsDownloadConfirmed = false;

    /// <summary>Re-evaluates the C64 directory and updates <see cref="Assessment"/>.</summary>
    /// <param name="ct">A token to cancel (evaluation itself is synchronous).</param>
    public Task RefreshAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Assessment = _evaluator.Evaluate(_c64Directory, _profile);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Picks a file and imports it as the ROM for <paramref name="role"/>. The file is rejected
    /// (leaving provisioning state unchanged and writing nothing) if it exceeds
    /// <see cref="MaxImportBytes"/>, is the wrong size, or fails the target spec's MD5. On
    /// success the bytes are written into the C64 directory under the spec's file name and the
    /// assessment is refreshed.
    /// </summary>
    /// <param name="role">The role the picked file should satisfy.</param>
    /// <param name="ct">A token to cancel the import.</param>
    public async Task ImportAsync(RomRole role, CancellationToken ct = default)
    {
        if (!_evaluator.Catalog.TryGetSpec(role, out var spec))
        {
            StatusMessage = $"No catalog entry for {role}.";
            return;
        }

        var picked = await _picker.PickAsync(ct).ConfigureAwait(false);
        if (picked is null)
        {
            StatusMessage = "Import cancelled.";
            return;
        }

        // Ceiling guard first: reject on the REPORTED length before reading any bytes.
        if (picked.Length > MaxImportBytes)
        {
            StatusMessage = $"File too large: {picked.Name} exceeds the {MaxImportBytes:N0}-byte import limit.";
            return;
        }

        var data = await picked.ReadBytesAsync(ct).ConfigureAwait(false);

        if (data.LongLength > MaxImportBytes)
        {
            StatusMessage = $"File too large: {picked.Name} exceeds the {MaxImportBytes:N0}-byte import limit.";
            return;
        }

        if (data.Length != spec.ExpectedSize)
        {
            StatusMessage = $"Wrong size for {spec.FileName}: expected {spec.ExpectedSize} bytes, got {data.Length}.";
            return;
        }

        var actualMd5 = Convert.ToHexString(MD5.HashData(data));
        if (!string.Equals(actualMd5, spec.ExpectedMd5, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = $"Checksum mismatch for {spec.FileName}; the file was not imported.";
            return;
        }

        Directory.CreateDirectory(_c64Directory);
        var destination = Path.Combine(_c64Directory, spec.FileName);
        await File.WriteAllBytesAsync(destination, data, ct).ConfigureAwait(false);

        StatusMessage = $"Imported {spec.FileName}.";
        await RefreshAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the confirm-gated verified download of the three core ROMs. It is a no-op unless
    /// <see cref="IsDownloadConfirmed"/> is set (via <see cref="ConfirmDownload"/>); once run, the
    /// confirmation is cleared and the assessment is refreshed.
    /// </summary>
    /// <param name="ct">A token to cancel the download.</param>
    public async Task DownloadAsync(CancellationToken ct = default)
    {
        if (!IsDownloadConfirmed)
        {
            // Confirm gate: downloading without an explicit confirm does nothing.
            StatusMessage = "Confirm the download before it can start.";
            return;
        }

        IsDownloadConfirmed = false;

        RomDownloadResult result;
        try
        {
            result = await _acquirer.DownloadCoreSetAsync(_c64Directory, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Download cancelled.";
            throw;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
            await RefreshAsync(ct).ConfigureAwait(false);
            return;
        }

        StatusMessage = result.Message;
        await RefreshAsync(ct).ConfigureAwait(false);
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
