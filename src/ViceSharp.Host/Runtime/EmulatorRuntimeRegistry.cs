using System.Diagnostics.CodeAnalysis;

namespace ViceSharp.Host.Runtime;

public sealed class EmulatorRuntimeRegistry
{
    private readonly Dictionary<string, EmulatorRuntimeSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();

    public void Add(EmulatorRuntimeSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_syncRoot)
        {
            _sessions.Add(session.SessionId, session);
        }
    }

    /// <summary>Snapshot of all live sessions (for the emulation pump to iterate without holding the registry lock during frame work).</summary>
    public EmulatorRuntimeSession[] Snapshot()
    {
        lock (_syncRoot)
        {
            return _sessions.Values.ToArray();
        }
    }

    public bool TryGet(string sessionId, [NotNullWhen(true)] out EmulatorRuntimeSession? session)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            session = null;
            return false;
        }

        lock (_syncRoot)
        {
            return _sessions.TryGetValue(sessionId, out session);
        }
    }

    public bool Remove(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        lock (_syncRoot)
        {
            return _sessions.Remove(sessionId);
        }
    }

    public void Replace(EmulatorRuntimeSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_syncRoot)
        {
            _sessions[session.SessionId] = session;
        }
    }
}
