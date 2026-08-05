namespace ViceSharp.Library.ViewModels;

/// <summary>FR-ROMM-CONN-001. The connection state surfaced to the UI.</summary>
public enum ConnectionState
{
    /// <summary>Not connected.</summary>
    Disconnected = 0,

    /// <summary>Connected and authenticated.</summary>
    Connected = 1,

    /// <summary>The saved credential is no longer valid; the user must re-authenticate.</summary>
    ReauthRequired = 2,
}

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-04). Tracks the RomM connection state and, on a 401, moves to a
/// "sign-in expired" reauth state and raises <see cref="ConnectionInvalid"/> so the UI can prompt for
/// re-authentication.
/// </summary>
public sealed class LibraryConnectionViewModel : LibraryObservableObject
{
    private ConnectionState _state = ConnectionState.Disconnected;

    /// <summary>The current connection state.</summary>
    public ConnectionState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    /// <summary>AC-CONN-04. Raised when the connection becomes invalid (401).</summary>
    public event EventHandler? ConnectionInvalid;

    /// <summary>Marks the connection as authenticated.</summary>
    public void MarkConnected() => State = ConnectionState.Connected;

    /// <summary>Marks the connection as disconnected.</summary>
    public void MarkDisconnected() => State = ConnectionState.Disconnected;

    /// <summary>
    /// AC-CONN-04. Handles an unauthorized (401) response: moves to <see cref="ConnectionState.ReauthRequired"/>
    /// and raises <see cref="ConnectionInvalid"/>.
    /// </summary>
    public void HandleUnauthorized()
    {
        State = ConnectionState.ReauthRequired;
        Dispatch(() => ConnectionInvalid?.Invoke(this, EventArgs.Empty));
    }
}
