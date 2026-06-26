using System.Collections.ObjectModel;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

public sealed class AttachSlotViewModel : ObservableObject
{
    private string _filePath = string.Empty;
    private string _statusText = "Empty";
    private string _validationError = string.Empty;
    private bool _isAttached;
    private bool _isReadOnly;
    private bool _isIecBusActive;
    private bool _trueDrive;
    private bool _ledOn;

    public AttachSlotViewModel(MediaSlot slot, string title, string mediaKind, string[] filePatterns, bool trueDrive = false)
    {
        Slot = slot;
        Title = title;
        MediaKind = mediaKind;
        FilePatterns = filePatterns;
        _trueDrive = trueDrive && SupportsTrueDrive;
    }

    public MediaSlot Slot { get; }

    public string Title { get; }

    public string MediaKind { get; }

    public string[] FilePatterns { get; }

    public ObservableCollection<string> RecentFiles { get; } = new();

    public string FilePath
    {
        get => _filePath;
        private set => SetProperty(ref _filePath, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationError);

    public bool IsAttached
    {
        get => _isAttached;
        private set => SetProperty(ref _isAttached, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    /// <summary>True for IEC drive slots that can switch between a simulated
    /// (buffered) and an emulated (true-drive) 1541 - i.e. where the True Drive
    /// toggle applies.</summary>
    public bool SupportsTrueDrive => Slot is MediaSlot.Drive8 or MediaSlot.Drive9;

    /// <summary>
    /// When true, this drive runs as a cycle-accurate emulated 1541 (6502 + VIA
    /// + DOS ROM over the IEC bus); when false, a lightweight simulated drive
    /// serves sectors directly. Mirrors VICE's per-unit
    /// DriveTrueEmulation / the Fidelity TrueDevice vs Buffered selector.
    /// </summary>
    public bool TrueDrive
    {
        get => _trueDrive;
        set => SetProperty(ref _trueDrive, value);
    }

    public bool IsIecBusActive
    {
        get => _isIecBusActive;
        private set
        {
            if (SetProperty(ref _isIecBusActive, value))
                OnPropertyChanged(nameof(IecActivityText));
        }
    }

    public string IecActivityText => IsIecBusActive ? "IEC Active" : "IEC Idle";

    /// <summary>
    /// FR-DRVLED-001: per-drive activity LED. Today it tracks IEC bus activity as
    /// a stand-in (<see cref="SetIecActivity"/>); <see cref="SetDriveLed"/> is the
    /// hook for the faithful VIA2 port B bit 3 source (the 1541 DOS ROM LED) once
    /// per-drive true-drive telemetry is plumbed through the host. Only drives
    /// (<see cref="SupportsTrueDrive"/>) ever light.
    /// </summary>
    public bool LedOn
    {
        get => _ledOn;
        private set => SetProperty(ref _ledOn, value);
    }

    /// <summary>
    /// Set the faithful drive LED state (VIA2 PB3) from host telemetry when the
    /// true-drive 1541 is active. No-op for non-drive slots. Not yet driven by a
    /// production caller (true-drive LED telemetry is a follow-up).
    /// </summary>
    public void SetDriveLed(bool on)
    {
        if (Slot is MediaSlot.Drive8 or MediaSlot.Drive9)
            LedOn = on;
    }

    public void ApplyAttachment(MediaAttachmentDto attachment, string recentFilePath = "")
    {
        FilePath = attachment.FilePath;
        IsAttached = attachment.IsAttached;
        IsReadOnly = attachment.IsReadOnly;
        ValidationError = attachment.Error;
        StatusText = attachment.IsAttached
            ? $"{CreateDisplayName(attachment)}{(attachment.AppliedToRuntime ? "" : " staged")}"
            : "Empty";

        OnPropertyChanged(nameof(HasValidationError));
        AddRecentFile(string.IsNullOrWhiteSpace(recentFilePath) ? attachment.FilePath : recentFilePath);
    }

    private static string CreateDisplayName(MediaAttachmentDto attachment)
        => string.IsNullOrWhiteSpace(attachment.DisplayName)
            ? Path.GetFileName(attachment.FilePath)
            : attachment.DisplayName;

    public void MarkEmpty()
    {
        FilePath = string.Empty;
        IsAttached = false;
        ValidationError = string.Empty;
        StatusText = "Empty";
        OnPropertyChanged(nameof(HasValidationError));
    }

    public void MarkError(string message)
    {
        ValidationError = message;
        StatusText = "Error";
        OnPropertyChanged(nameof(HasValidationError));
    }

    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        for (var i = RecentFiles.Count - 1; i >= 0; i--)
        {
            if (string.Equals(RecentFiles[i], filePath, StringComparison.OrdinalIgnoreCase))
                RecentFiles.RemoveAt(i);
        }

        RecentFiles.Insert(0, filePath);

        while (RecentFiles.Count > 6)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
    }

    public void SetIecActivity(bool isActive)
    {
        if (Slot is not (MediaSlot.Drive8 or MediaSlot.Drive9))
            isActive = false;

        IsIecBusActive = isActive;

        // Simulated-drive proxy for the activity LED: light it while the drive
        // is using the IEC bus. The faithful VIA2 PB3 source (SetDriveLed)
        // overrides this when true-drive telemetry is available.
        LedOn = isActive;
    }
}
