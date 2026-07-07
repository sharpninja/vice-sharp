namespace ViceSharp.TestHarness;

using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR: FR-HOST-SESSION, TR: TR-HOST-SESSIONHEAL-001. When the host reports
/// that the client's session no longer exists (external shutdown, host-side
/// restart, another client disposing it), the UI client must drop its cached
/// session id so the next command transparently creates a fresh session,
/// instead of wedging every subsequent call (and the status bar) on
/// "Emulator session '...' was not found." forever.
/// </summary>
public sealed class GrpcSessionLossRecoveryTests
{
    /// <summary>
    /// FR: FR-HOST-SESSION, TR: TR-HOST-SESSIONHEAL-001, TEST: TEST-HOST-SESSIONHEAL-01.
    /// Use case: classify host responses that mean "your session is gone".
    /// Acceptance: a NotFound status whose message names the client's current
    /// session id indicates loss; Ok, other codes, other-session NotFound,
    /// null status, and an empty local session id do not.
    /// </summary>
    [Fact]
    public void IndicatesLostSession_Classifies_NotFound_For_Current_Session()
    {
        const string sessionId = "emulator-e484ba7927a04600bae8e6163fcbf06c";
        var lost = RpcStatus.NotFound($"Emulator session '{sessionId}' was not found.");

        Assert.True(GrpcHostProtocolClient.IndicatesLostSession(lost, sessionId));
        Assert.False(GrpcHostProtocolClient.IndicatesLostSession(RpcStatus.Ok(), sessionId));
        Assert.False(GrpcHostProtocolClient.IndicatesLostSession(RpcStatus.InvalidArgument("nope"), sessionId));
        Assert.False(GrpcHostProtocolClient.IndicatesLostSession(
            RpcStatus.NotFound("Emulator session 'emulator-other' was not found."), sessionId));
        Assert.False(GrpcHostProtocolClient.IndicatesLostSession(null, sessionId));
        Assert.False(GrpcHostProtocolClient.IndicatesLostSession(lost, string.Empty));
        Assert.False(GrpcHostProtocolClient.IndicatesLostSession(
            RpcStatus.NotFound("Capture 'capture-1' was not found."), sessionId));
    }
}
