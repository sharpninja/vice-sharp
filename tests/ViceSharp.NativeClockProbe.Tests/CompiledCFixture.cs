using System.Diagnostics;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>Native-free C fixture build/run helper shared by wrapper and gate contracts.</summary>
internal sealed class CompiledCFixture
{
    private readonly string compiler = Environment.GetEnvironmentVariable("VICE_NATIVE_CC")
        ?? (OperatingSystem.IsWindows() ? @"C:\msys64\mingw64\bin\gcc.exe" : "/usr/bin/gcc");
    private readonly string directory;
    private readonly Lazy<string> executable;

    internal CompiledCFixture(string name, string[] sources, params string[] linkerArguments)
    {
        directory = Path.Combine(AppContext.BaseDirectory, name + "-evidence");
        executable = new Lazy<string>(() =>
        {
            Directory.CreateDirectory(directory);
            if (!File.Exists(compiler))
                throw new FileNotFoundException("GNU-compatible C compiler is mandatory; set VICE_NATIVE_CC.", compiler);
            var output = Path.Combine(directory, name + (OperatingSystem.IsWindows() ? ".exe" : ""));
            string[] arguments = ["-std=c11", "-Wall", "-Wextra", "-Werror", "-O0", "-g",
                .. linkerArguments, .. sources.Select(source => Path.Combine(AppContext.BaseDirectory, "Fixtures", source)),
                "-o", output];
            var result = Execute(compiler, arguments, "compile");
            if (result.ExitCode != 0 || !File.Exists(output))
                throw new InvalidOperationException($"C fixture compile failed ({result.ExitCode}): {result.Error}");
            return output;
        });
    }

    internal (int ExitCode, string Output, string Error) Run(string[] arguments) =>
        Execute(executable.Value, arguments, "run-" + Guid.NewGuid().ToString("N"));

    private (int ExitCode, string Output, string Error) Execute(string path, string[] arguments, string receiptName)
    {
        var start = new ProcessStartInfo(path)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["PATH"] = Path.GetDirectoryName(compiler) + Path.PathSeparator + start.Environment["PATH"];
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Child process did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        var timedOut = !process.WaitForExit(30_000);
        if (timedOut)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5_000);
        }
        var streams = Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        File.WriteAllText(Path.Combine(directory, receiptName + ".log"),
            $"{DateTimeOffset.Now:O}\nExecutable: {path}\nArguments: {string.Join(" | ", arguments)}\n" +
            $"ExitCode: {process.ExitCode}\nTimedOut: {timedOut}\nSTDOUT:\n{streams[0]}\nSTDERR:\n{streams[1]}");
        if (timedOut) throw new TimeoutException("C fixture/compiler timed out; this is not a passing gate.");
        return (process.ExitCode, streams[0], streams[1]);
    }
}
