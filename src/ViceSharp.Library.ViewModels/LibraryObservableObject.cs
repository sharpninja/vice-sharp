using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ViceSharp.Library.ViewModels;

/// <summary>
/// TR-ROMM-THREAD-001 (AC-BROWSE-08). Base <see cref="INotifyPropertyChanged"/> for library
/// ViewModels. It captures the UI <see cref="SynchronizationContext"/> at construction and dispatches
/// <see cref="PropertyChanged"/> (and caller actions via <see cref="Dispatch"/>) to it when raised
/// from a background thread, so a XAML binding never marshals the notification off the UI thread
/// (<c>RPC_E_WRONG_THREAD</c> / 0x8001010E). When already on that context, or when none was captured
/// (headless tests), it raises inline. Copied from the proven <c>XboxRomProvisioningViewModel</c>
/// dispatch pattern.
/// </summary>
public abstract class LibraryObservableObject : INotifyPropertyChanged
{
    // Captured at construction (the UI dispatcher's context in a head; typically null in headless
    // tests). Background continuations dispatch notifications here.
    private readonly SynchronizationContext? _sync;

    /// <summary>
    /// Captures the current <see cref="SynchronizationContext"/> (the UI dispatcher when constructed
    /// on the UI thread; <c>null</c> in headless tests).
    /// </summary>
    protected LibraryObservableObject() => _sync = SynchronizationContext.Current;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets <paramref name="field"/> and raises <see cref="PropertyChanged"/> when the value changes.
    /// </summary>
    /// <returns><c>true</c> when the value changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for <paramref name="propertyName"/>, dispatching to the
    /// captured context when raised from another thread.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChangedEventHandler? handler = PropertyChanged;
        if (handler is null)
        {
            return;
        }

        Dispatch(() => handler(this, new PropertyChangedEventArgs(propertyName)));
    }

    /// <summary>
    /// Runs <paramref name="action"/> on the captured UI context (used for observable-collection
    /// mutations from a background load), inline when already on that context or when none was
    /// captured.
    /// </summary>
    /// <param name="action">The work to run on the UI context.</param>
    protected void Dispatch(Action action)
    {
        if (_sync is null || SynchronizationContext.Current == _sync)
        {
            action();
        }
        else
        {
            _sync.Post(_ => action(), null);
        }
    }
}
