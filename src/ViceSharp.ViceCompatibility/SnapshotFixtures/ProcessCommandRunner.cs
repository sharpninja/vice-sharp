using System.Diagnostics;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The production <see cref="ICommandRunner"/>: starts the observer with redirected streams, writes every stderr
/// line to the request's stderr file, keeps the <c>VICE_SNAP</c> records, drains stdout, and stops the process
/// when the wall-clock budget expires.
/// </summary>
public sealed class ProcessCommandRunner : ICommandRunner
{
    /// <inheritdoc />
    public CommandResult Run(CommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var start = new ProcessStartInfo(request.FileName)
        {
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in request.Arguments)
            start.ArgumentList.Add(argument);
        foreach (var (key, value) in request.Environment)
            start.Environment[key] = value;

        var snapshotLines = new List<string>();
        Directory.CreateDirectory(Path.GetDirectoryName(request.StderrPath)!);
        using var stderrWriter = new StreamWriter(request.StderrPath, append: false);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start '{request.FileName}'.");
        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = Task.Run(() =>
        {
            while (process.StandardError.ReadLine() is { } line)
            {
                stderrWriter.WriteLine(line);
                if (line.StartsWith("VICE_SNAP,", StringComparison.Ordinal))
                    snapshotLines.Add(line);
            }
        });

        var timedOut = false;
        if (!process.WaitForExit(request.TimeoutMilliseconds))
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            process.WaitForExit();
        }
        stderrTask.Wait();
        stdoutTask.Wait();
        stderrWriter.Flush();
        return new CommandResult(timedOut ? -1 : process.ExitCode, timedOut, snapshotLines);
    }
}
