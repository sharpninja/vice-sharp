using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Input;

namespace ViceSharp.Host.Services;

public sealed class InputServiceHost : IInputService
{
    private readonly EmulatorRuntimeRegistry _registry;

    public InputServiceHost(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ValueTask<InputCommandResponse> SetKeyStateAsync(
        SetKeyStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new InputCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        if (string.IsNullOrWhiteSpace(request.Key))
            return ValueTask.FromResult(new InputCommandResponse(RpcStatus.InvalidArgument("Key is required."), null));

        lock (session.SyncRoot)
        {
            var appliedToRuntime = ApplyKeyStateToRuntime(session, request.Key, request.IsPressed);
            session.KeyStates[request.Key] = new KeyStateDto(
                request.Key,
                request.IsPressed,
                appliedToRuntime,
                request.PhysicalKey,
                request.Text,
                request.Modifiers);
            return ValueTask.FromResult(new InputCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToInputStateDto(session)));
        }
    }

    public ValueTask<InputCommandResponse> SetJoystickStateAsync(
        SetJoystickStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new InputCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        if (!IsJoystickInputPort(request.Port))
            return ValueTask.FromResult(new InputCommandResponse(RpcStatus.InvalidArgument("Keyboard is not a joystick port."), null));

        lock (session.SyncRoot)
        {
            var appliedToRuntime = ApplyJoystickStateToRuntime(session, request.Port, request.DirectionMask, request.FireButton);
            session.JoystickStates[request.Port] = new JoystickStateDto(request.DirectionMask, request.FireButton, appliedToRuntime);
            return ValueTask.FromResult(new InputCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToInputStateDto(session)));
        }
    }

    public ValueTask<GetInputStateResponse> GetInputStateAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new GetInputStateResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(new GetInputStateResponse(RpcStatus.Ok(), HostProtocolMapper.ToInputStateDto(session)));
        }
    }

    public ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new ListKeyboardMapsResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), []));

        lock (session.SyncRoot)
        {
            var maps = EnumerateKeyboardMaps(session.SelectedKeyboardMapId)
                .ToArray();
            session.SelectedKeyboardMap ??= maps.FirstOrDefault(map => map.IsSelected) ?? maps.FirstOrDefault();
            if (session.SelectedKeyboardMap is not null)
                session.SelectedKeyboardMapId = session.SelectedKeyboardMap.Id;

            return ValueTask.FromResult(new ListKeyboardMapsResponse(RpcStatus.Ok(), maps));
        }
    }

    public ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        SetKeyboardMapRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new KeyboardMapResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        if (string.IsNullOrWhiteSpace(request.KeyboardMapId) && request.Payload is null)
            return ValueTask.FromResult(new KeyboardMapResponse(RpcStatus.InvalidArgument("KeyboardMapId or Payload is required."), null));

        lock (session.SyncRoot)
        {
            var selected = request.Payload is { Length: > 0 }
                ? CreateCustomKeyboardMapDto(request)
                : EnumerateKeyboardMaps(request.KeyboardMapId).FirstOrDefault(map => string.Equals(map.Id, request.KeyboardMapId, StringComparison.OrdinalIgnoreCase));

            if (selected is null)
                return ValueTask.FromResult(new KeyboardMapResponse(RpcStatus.NotFound($"Keyboard map '{request.KeyboardMapId}' was not found."), null));

            var applyResult = ApplyKeyboardMapToRuntime(session, request, selected, out var runtimeError);
            if (applyResult == KeyboardMapApplyResult.Invalid)
            {
                return ValueTask.FromResult(new KeyboardMapResponse(
                    RpcStatus.InvalidArgument(runtimeError),
                    session.SelectedKeyboardMap,
                    HostProtocolMapper.ToInputStateDto(session)));
            }

            if (applyResult == KeyboardMapApplyResult.NotAvailable && !string.IsNullOrWhiteSpace(runtimeError))
                selected = selected with { Error = runtimeError };

            session.SelectedKeyboardMapId = selected.Id;
            session.SelectedKeyboardMap = selected with { IsSelected = true };
            return ValueTask.FromResult(new KeyboardMapResponse(
                RpcStatus.Ok(),
                session.SelectedKeyboardMap,
                HostProtocolMapper.ToInputStateDto(session)));
        }
    }

    private static bool ApplyKeyStateToRuntime(EmulatorRuntimeSession session, string key, bool isPressed)
    {
        var keyboardInput = session.Machine.Devices.All.OfType<IMachineKeyboardInput>().FirstOrDefault();
        if (keyboardInput is null)
            return false;

        var wasPressed = session.KeyStates.TryGetValue(key, out var previous) && previous.IsPressed;
        if (wasPressed == isPressed)
            return true;

        return keyboardInput.SetKeyState(key, isPressed);
    }

    private static bool ApplyJoystickStateToRuntime(
        EmulatorRuntimeSession session,
        InputPort port,
        byte directionMask,
        bool fireButton)
    {
        var joystickInput = session.Machine.Devices.All.OfType<IMachineJoystickInput>().FirstOrDefault();
        if (joystickInput is null)
            return false;

        var controlPort = ToRuntimeControlPort(ResolveRuntimeJoystickPort(session, port));
        return controlPort != 0 && joystickInput.SetJoystickState(controlPort, directionMask, fireButton);
    }

    private static bool IsJoystickInputPort(InputPort port)
        => port is InputPort.Joystick1 or InputPort.Joystick2 or InputPort.PrimaryJoystick;

    private static InputPort ResolveRuntimeJoystickPort(EmulatorRuntimeSession session, InputPort requestedPort)
    {
        if (requestedPort != InputPort.PrimaryJoystick)
            return requestedPort;

        return session.InputSettings.SwapJoystickPorts
            ? SwapJoystickPort(session.InputSettings.PrimaryJoystickPort)
            : session.InputSettings.PrimaryJoystickPort;
    }

    private static InputPort SwapJoystickPort(InputPort port)
        => port switch
        {
            InputPort.Joystick1 => InputPort.Joystick2,
            InputPort.Joystick2 => InputPort.Joystick1,
            _ => port
        };

    private static int ToRuntimeControlPort(InputPort port)
    {
        return port switch
        {
            InputPort.Joystick1 => 1,
            InputPort.Joystick2 => 2,
            _ => 0
        };
    }

    private static KeyboardMapApplyResult ApplyKeyboardMapToRuntime(
        EmulatorRuntimeSession session,
        SetKeyboardMapRequest request,
        KeyboardMapDto selected,
        out string error)
    {
        var mapSelection = session.Machine.Devices.All.OfType<IKeyboardInputMapSelection>().FirstOrDefault();
        if (mapSelection is null)
        {
            error = "The current machine accepts key states but does not expose runtime keymap switching yet.";
            return KeyboardMapApplyResult.NotAvailable;
        }

        if (!TryLoadKeyboardMap(request, selected, out var keyboardMap, out error))
            return KeyboardMapApplyResult.Invalid;

        mapSelection.SelectKeyboardMap(keyboardMap);
        error = string.Empty;
        return KeyboardMapApplyResult.Applied;
    }

    private static bool TryLoadKeyboardMap(
        SetKeyboardMapRequest request,
        KeyboardMapDto selected,
        out IKeyboardInputMap keyboardMap,
        out string error)
    {
        keyboardMap = C64HostKeyboardMapper.DefaultFallbackMap;
        var temporaryPath = string.Empty;
        var path = selected.SourcePath;

        try
        {
            if (request.Payload is { Length: > 0 })
            {
                temporaryPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.vkm");
                File.WriteAllBytes(temporaryPath, request.Payload);
                path = temporaryPath;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                error = selected.Error;
                return false;
            }

            var result = C64VkmParser.Load(path);
            if (result.HasErrors)
            {
                error = string.Join(
                    Environment.NewLine,
                    result.Diagnostics
                        .Where(diagnostic => diagnostic.Severity == C64VkmDiagnosticSeverity.Error)
                        .Select(diagnostic => $"{diagnostic.Path}:{diagnostic.LineNumber}: {diagnostic.Message}"));
                return false;
            }

            keyboardMap = result.KeyboardMap;
            error = string.Empty;
            return true;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch
                {
                    // Temporary VKM cleanup is best-effort.
                }
            }
        }
    }

    private static IEnumerable<KeyboardMapDto> EnumerateKeyboardMaps(string selectedId)
    {
        var c64Path = FindViceC64DataPath();
        var foundAny = false;

        if (c64Path is not null)
        {
            foreach (var path in Directory.EnumerateFiles(c64Path, "*.vkm").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                foundAny = true;
                var name = Path.GetFileNameWithoutExtension(path);
                var id = $"c64:{name}";
                yield return new KeyboardMapDto(
                    id,
                    CreateKeyboardMapDisplayName(name),
                    "C64",
                    name.Contains("sym", StringComparison.OrdinalIgnoreCase) ? "Symbolic" : "Positional",
                    path,
                    string.Equals(id, selectedId, StringComparison.OrdinalIgnoreCase),
                    true);
            }
        }

        if (!foundAny)
        {
            yield return new KeyboardMapDto(
                "c64:gtk3_pos",
                "GTK3 positional",
                "C64",
                "Positional",
                string.Empty,
                string.Equals("c64:gtk3_pos", selectedId, StringComparison.OrdinalIgnoreCase),
                true,
                "VICE keymap files were not found; using the embedded fallback map.");
        }
    }

    /// <summary>
    /// Finds the VICE C64 data directory containing *.vkm files.
    /// Delegates to ViceDataPathResolver (same logic used by the ROM loader)
    /// then appends the "C64" subdirectory.
    /// </summary>
    private static string? FindViceC64DataPath()
    {
        foreach (var dataRoot in ViceSharp.RomFetch.ViceDataPathResolver.FindDataRoots())
        {
            var candidate = Path.Combine(dataRoot, "C64");
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.vkm").Any())
                return candidate;
        }

        return null;
    }

    private static KeyboardMapDto CreateCustomKeyboardMapDto(SetKeyboardMapRequest request)
    {
        var id = string.IsNullOrWhiteSpace(request.KeyboardMapId)
            ? $"custom:{Guid.NewGuid():N}"
            : request.KeyboardMapId;
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? Path.GetFileName(request.SourcePath)
            : request.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Custom VKM";

        return new KeyboardMapDto(id, displayName, "C64", "Custom", request.SourcePath, true, false);
    }

    private static string CreateKeyboardMapDisplayName(string fileName)
    {
        return fileName
            .Replace("gtk3_", "GTK3 ", StringComparison.OrdinalIgnoreCase)
            .Replace("sdl_", "SDL ", StringComparison.OrdinalIgnoreCase)
            .Replace("_", " ", StringComparison.Ordinal)
            .Trim();
    }

    private enum KeyboardMapApplyResult
    {
        Applied,
        NotAvailable,
        Invalid
    }
}
