using System.Runtime.CompilerServices;

namespace ViceSharp.Core;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 load detector (F03; round 4 N11, round 5 N16 and N17, round 7 N24). When the environment
/// variable <see cref="EnvironmentVariable"/> names a file, every attempt of <see cref="ViceNative"/> and
/// <see cref="ViceNativeXvic"/> to resolve, probe or load a native library appends one line
/// <c>&lt;library&gt;,&lt;resolved path or none&gt;,&lt;outcome&gt;</c> to that file, where the outcome is
/// <see cref="Resolved"/> or <see cref="Unresolved"/> for the path probe behind IsAvailable, AvailabilityMessage and
/// the create paths, <see cref="Loaded"/> or <see cref="Failed"/> for NativeLibrary.Load, and <see cref="Fallback"/>
/// when the combined resolver returns nothing and the runtime's own search applies. When the variable is unset the
/// sink is null, <see cref="Record"/> returns after one static read, and no allocation happens. An unwritable file
/// fails the attempt loudly. The per-cycle step paths never call this type.
/// </summary>
internal static class NativeLoadAudit
{
    /// <summary>Environment variable naming the audit log file.</summary>
    internal const string EnvironmentVariable = "VICESHARP_NATIVE_LOAD_LOG";

    internal const string Resolved = "resolved";
    internal const string Unresolved = "unresolved";
    internal const string Loaded = "loaded";
    internal const string Failed = "failed";
    internal const string Fallback = "fallback";

    /// <summary>Receives one record per attempt. Production uses <see cref="FileSink"/>; tests substitute doubles.</summary>
    internal interface ISink
    {
        void Record(string library, string? path, string outcome);
    }

    /// <summary>Appends records to the file named by the environment variable; throws when the file is unwritable.</summary>
    internal sealed class FileSink : ISink
    {
        private readonly string _path;
        private readonly object _gate = new();

        public FileSink(string path)
        {
            _path = path;
            try
            {
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw new InvalidOperationException($"{EnvironmentVariable} names an unwritable file '{path}'.", e);
            }
        }

        public void Record(string library, string? path, string outcome)
        {
            var line = FormatLine(library, path, outcome);
            lock (_gate)
            {
                try
                {
                    File.AppendAllText(_path, line);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    throw new InvalidOperationException($"{EnvironmentVariable} file '{_path}' could not be appended.", e);
                }
            }
        }
    }

    private static ISink? _sink = CreateFromEnvironment();

    private static ISink? CreateFromEnvironment()
    {
        var path = Environment.GetEnvironmentVariable(EnvironmentVariable);
        return string.IsNullOrWhiteSpace(path) ? null : new FileSink(path);
    }

    /// <summary>True when a sink is installed (the variable was set, or a test substituted a double).</summary>
    internal static bool IsEnabled => _sink is not null;

    /// <summary>The one line format: library, resolved path or none, outcome, newline.</summary>
    internal static string FormatLine(string library, string? path, string outcome)
        => string.Concat(library, ",", path ?? "none", ",", outcome, "\n");

    /// <summary>Records one attempt; returns immediately without allocating when no sink is installed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Record(string library, string? path, string outcome)
    {
        var sink = _sink;
        if (sink is null)
            return;
        sink.Record(library, path, outcome);
    }

    /// <summary>Test seam: installs a sink (or null) and returns the previous one so the caller can restore it.</summary>
    internal static ISink? Replace(ISink? sink)
    {
        var previous = _sink;
        _sink = sink;
        return previous;
    }
}
