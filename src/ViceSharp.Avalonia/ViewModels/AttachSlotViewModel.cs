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

    public AttachSlotViewModel(MediaSlot slot, string title, string mediaKind, string[] filePatterns)
    {
        Slot = slot;
        Title = title;
        MediaKind = mediaKind;
        FilePatterns = filePatterns;
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

    public void ApplyAttachment(MediaAttachmentDto attachment)
    {
        FilePath = attachment.FilePath;
        IsAttached = attachment.IsAttached;
        IsReadOnly = attachment.IsReadOnly;
        ValidationError = attachment.Error;
        StatusText = attachment.IsAttached
            ? $"{Path.GetFileName(attachment.FilePath)}{(attachment.AppliedToRuntime ? "" : " staged")}"
            : "Empty";

        OnPropertyChanged(nameof(HasValidationError));
        AddRecentFile(attachment.FilePath);
    }

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
}
