using System.Globalization;
using System.Text;

namespace ViceSharp.Avalonia.Persistence;

/// <summary>
/// Reads and writes <see cref="PersistedState"/> to a UI-owned settings file
/// (<c>vice-sharp-ui.ini</c>) in the VICE config folder. This is a self-contained
/// key/value store with no dependency on the emulator runtime: the Avalonia app
/// is a pure protocol/host client, so persisted settings are re-applied to the
/// host through the protocol on startup rather than written to the host's own
/// resource files. The config directory defaults to the per-user app-data VICE
/// folder but can be overridden for testing.
/// </summary>
public sealed class SessionPersistence
{
    private const string FileName = "vice-sharp-ui.ini";

    private const string KeySaveSettings = "SaveSettingsOnExit";
    private const string KeySaveTransient = "SaveTransientValuesOnExit";

    private const string KeyLimiterRate = "SettingsLimiterRatePercent";
    private const string KeyLimiterEnabled = "SettingsLimiterEnabled";
    private const string KeyMachineProfile = "SettingsMachineProfileId";
    private const string KeyRenderer = "SettingsRenderer"; // also the "settings present" marker
    private const string KeyDisplayScale = "SettingsDisplayScale";
    private const string KeyCropMode = "SettingsCropMode";
    private const string KeyAspectMode = "SettingsAspectMode";
    private const string KeyPalette = "SettingsPalette";
    private const string KeyAudioMode = "SettingsAudioMode";
    private const string KeyInputMode = "SettingsInputMode";
    private const string KeyPrimaryJoystick = "SettingsPrimaryJoystickPort";
    private const string KeySwapJoysticks = "SettingsSwapJoystickPorts";
    private const string KeyResourceMode = "SettingsResourceMode";
    private const string KeyDockSide = "SettingsDockSide";
    private const string KeyPacingStrategy = "SettingsPacingStrategy";
    private const string KeyMasterVolume = "SettingsMasterVolumePercent";
    private const string KeyMuted = "SettingsMuted";

    private const string KeyKeyboardMapId = "TransientKeyboardMapId";
    private const string KeyKeyboardMapSource = "TransientKeyboardMapSource";
    private const string KeyAttachmentCount = "TransientAttachmentCount";

    private readonly string _directory;
    private readonly string _filePath;

    public SessionPersistence()
        : this(DefaultDirectory())
    {
    }

    public SessionPersistence(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _directory = configDirectory;
        _filePath = Path.Combine(configDirectory, FileName);
    }

    public PersistedState Load()
    {
        var map = ReadFile();

        var saveSettings = ReadBool(map, KeySaveSettings);
        var saveTransient = ReadBool(map, KeySaveTransient);

        return new PersistedState(
            saveSettings,
            saveTransient,
            saveSettings ? ReadSettings(map) : null,
            saveTransient ? ReadTransient(map) : null);
    }

    public void Save(PersistedState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Fresh map each save: stale keys never linger, so a disabled toggle is
        // honoured next launch with no special clearing.
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KeySaveSettings] = Bool(state.SaveSettingsOnExit),
            [KeySaveTransient] = Bool(state.SaveTransientValuesOnExit),
        };

        if (state.Settings is { } s)
            WriteSettings(map, s);
        if (state.Transient is { } t)
            WriteTransient(map, t);

        WriteFile(map);
    }

    private static PersistedSettings? ReadSettings(Dictionary<string, string> map)
    {
        if (!map.ContainsKey(KeyRenderer))
            return null; // settings were never written

        return new PersistedSettings(
            ReadDouble(map, KeyLimiterRate, 100),
            ReadBool(map, KeyLimiterEnabled, true),
            ReadString(map, KeyMachineProfile, "c64"),
            ReadString(map, KeyRenderer, "Host direct"),
            ReadString(map, KeyDisplayScale, "2x"),
            ReadString(map, KeyCropMode, "Visible area"),
            ReadString(map, KeyAspectMode, "VICE pixel aspect"),
            ReadString(map, KeyPalette, "VICE default"),
            ReadString(map, KeyAudioMode, "Enabled"),
            ReadString(map, KeyInputMode, "Keyboard + joystick"),
            ReadString(map, KeyPrimaryJoystick, "Joystick 2"),
            ReadBool(map, KeySwapJoysticks),
            ReadString(map, KeyResourceMode, "Auto detect"),
            ReadInt(map, KeyDockSide, 0))
        {
            PacingStrategy = ReadString(map, KeyPacingStrategy, "VICE"),
            MasterVolumePercent = ReadDouble(map, KeyMasterVolume, 100),
            Muted = ReadBool(map, KeyMuted),
        };
    }

    private static void WriteSettings(Dictionary<string, string> map, PersistedSettings v)
    {
        map[KeyLimiterRate] = v.LimiterRatePercent.ToString(CultureInfo.InvariantCulture);
        map[KeyLimiterEnabled] = Bool(v.LimiterEnabled);
        map[KeyMachineProfile] = v.MachineProfileId;
        map[KeyRenderer] = v.Renderer;
        map[KeyDisplayScale] = v.DisplayScale;
        map[KeyCropMode] = v.CropMode;
        map[KeyAspectMode] = v.AspectMode;
        map[KeyPalette] = v.Palette;
        map[KeyAudioMode] = v.AudioMode;
        map[KeyInputMode] = v.InputMode;
        map[KeyPrimaryJoystick] = v.PrimaryJoystickPort;
        map[KeySwapJoysticks] = Bool(v.SwapJoystickPorts);
        map[KeyResourceMode] = v.ResourceMode;
        map[KeyDockSide] = v.DockSide.ToString(CultureInfo.InvariantCulture);
        map[KeyPacingStrategy] = v.PacingStrategy;
        map[KeyMasterVolume] = v.MasterVolumePercent.ToString(CultureInfo.InvariantCulture);
        map[KeyMuted] = Bool(v.Muted);
    }

    private static PersistedTransient ReadTransient(Dictionary<string, string> map)
    {
        var count = ReadInt(map, KeyAttachmentCount, 0);
        var attachments = new List<PersistedAttachment>();
        for (var i = 0; i < count; i++)
        {
            var slot = ReadString(map, $"TransientAttachment{i}Slot", string.Empty);
            var file = ReadString(map, $"TransientAttachment{i}File", string.Empty);
            if (string.IsNullOrWhiteSpace(slot) || string.IsNullOrWhiteSpace(file))
                continue;

            attachments.Add(new PersistedAttachment(
                slot,
                file,
                ReadBool(map, $"TransientAttachment{i}ReadOnly"),
                ReadBool(map, $"TransientAttachment{i}TrueDrive")));
        }

        return new PersistedTransient(
            attachments,
            map.TryGetValue(KeyKeyboardMapId, out var id) && id.Length > 0 ? id : null,
            map.TryGetValue(KeyKeyboardMapSource, out var src) && src.Length > 0 ? src : null);
    }

    private static void WriteTransient(Dictionary<string, string> map, PersistedTransient v)
    {
        map[KeyAttachmentCount] = v.Attachments.Count.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < v.Attachments.Count; i++)
        {
            var a = v.Attachments[i];
            map[$"TransientAttachment{i}Slot"] = a.Slot;
            map[$"TransientAttachment{i}File"] = a.FilePath;
            map[$"TransientAttachment{i}ReadOnly"] = Bool(a.IsReadOnly);
            map[$"TransientAttachment{i}TrueDrive"] = Bool(a.TrueDrive);
        }

        if (!string.IsNullOrWhiteSpace(v.KeyboardMapId))
            map[KeyKeyboardMapId] = v.KeyboardMapId;
        if (!string.IsNullOrWhiteSpace(v.KeyboardMapSourcePath))
            map[KeyKeyboardMapSource] = v.KeyboardMapSourcePath;
    }

    private Dictionary<string, string> ReadFile()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_filePath))
            return map;

        foreach (var raw in File.ReadAllLines(_filePath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is ';' or '#' or '[')
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            map[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return map;
    }

    private void WriteFile(Dictionary<string, string> map)
    {
        Directory.CreateDirectory(_directory);

        var builder = new StringBuilder();
        builder.AppendLine("[ViceSharp]");
        foreach (var pair in map)
            builder.Append(pair.Key).Append('=').AppendLine(pair.Value);

        File.WriteAllText(_filePath, builder.ToString());
    }

    private static string DefaultDirectory()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vice");

    private static string ReadString(Dictionary<string, string> map, string key, string fallback)
        => map.TryGetValue(key, out var value) && value.Length > 0 ? value : fallback;

    private static bool ReadBool(Dictionary<string, string> map, string key, bool fallback = false)
        => map.TryGetValue(key, out var value)
            ? value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1"
            : fallback;

    private static double ReadDouble(Dictionary<string, string> map, string key, double fallback)
        => map.TryGetValue(key, out var value)
            && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static int ReadInt(Dictionary<string, string> map, string key, int fallback)
        => map.TryGetValue(key, out var value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static string Bool(bool value) => value ? "true" : "false";
}
