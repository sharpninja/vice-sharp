using System.Text;
using System.Text.Json;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.Host;

public static class DebugAttachInfoProvider
{
    public static string BuildClipboardText(string attachJson, EmulatorStatusDto? status)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ViceSharp Debug Attach Info");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(attachJson))
        {
            builder.AppendLine(attachJson.Trim());
            builder.AppendLine();
            AppendAttachSummary(builder, attachJson);
        }

        if (status is not null)
        {
            builder.AppendLine("Status:");
            builder.AppendLine($"SessionId: {status.SessionId}");
            builder.AppendLine($"RunState: {status.RunState}");
            builder.AppendLine($"Cycle: {status.Cycle}");
            builder.AppendLine($"FrameCount: {status.FrameCount}");
            builder.AppendLine($"FPS: {status.MeasuredFps:g}");
            builder.AppendLine($"ClockHz: {status.EffectiveClockHz:g}");
            builder.AppendLine($"ClockPercent: {status.EffectiveClockPercent:g}");
            builder.AppendLine($"PC: {status.Pc:X4}");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendAttachSummary(StringBuilder builder, string attachJson)
    {
        try
        {
            using var document = JsonDocument.Parse(attachJson);
            var root = document.RootElement;
            AppendIfPresent(builder, root, "endpoint", "Endpoint");
            AppendIfPresent(builder, root, "currentSessionId", "CurrentSessionId");
            AppendIfPresent(builder, root, "appVersion", "AppVersion");
            AppendIfPresent(builder, root, "protocolPackage", "ProtocolPackage");
            AppendIfPresent(builder, root, "authMode", "AuthMode");
            builder.AppendLine();
        }
        catch (JsonException)
        {
            builder.AppendLine("Attach JSON could not be parsed.");
            builder.AppendLine();
        }
    }

    private static void AppendIfPresent(StringBuilder builder, JsonElement root, string propertyName, string label)
    {
        if (root.TryGetProperty(propertyName, out var value))
            builder.AppendLine($"{label}: {value}");
    }
}
