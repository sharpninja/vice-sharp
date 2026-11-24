using System.Diagnostics;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Native-free C fixture build/run helper shared by wrapper and gate contracts. On Windows every compile runs
/// inside the MSYS2 MINGW64 login shell (Payton directive 2026-10-20T08:07, "always use msys to compile native
/// VICE code", extended to fixtures at 08:14); on Linux the system compiler runs directly. The resolved compiler
/// and MSYSTEM are recorded in compile.log so a receipt can prove where the build ran.
/// </summary>
internal sealed class CompiledCFixture
{
    /// <summary>A process invocation: executable, ordered arguments, extra environment, and a label for the log.</summary>
    internal sealed record Invocation(string FileName, string[] ArgumentList, IReadOnlyDictionary<string, string> Environment, string Description);

    private static readonly string MsysRoot = Environment.GetEnvironmentVariable("VICE_MSYS_ROOT") ?? @"C:\msys64";
    private static readonly string LinuxCompiler = Environment.GetEnvironmentVariable("VICE_NATIVE_CC") ?? "/usr/bin/gcc";
    private readonly string directory;
    private readonly Lazy<string> executable;

    /// <summary>Pure: builds the compile invocation for the platform without starting anything.</summary>
    internal static Invocation BuildCompileInvocation(bool isWindows, string msysRoot, string linuxCompiler, string[] compilerArguments)
    {
        if (!isWindows)
            return new(linuxCompiler, compilerArguments, new Dictionary<string, string>(), linuxCompiler);
        // Lab rule (Payton 2026-10-20T08:19): forward slashes on Windows, for the shell path and every path argument.
        const string script = "printf 'resolved compiler: %s\\nMSYSTEM=%s\\n' \"$(command -v gcc)\" \"$MSYSTEM\"; exec gcc \"$@\"";
        return new(msysRoot.Replace('\\', '/').TrimEnd('/') + "/usr/bin/bash.exe",
            ["-lc", script, "bash", .. compilerArguments.Select(a => a.Replace('\\', '/'))],
            new Dictionary<string, string> { ["MSYSTEM"] = "MINGW64", ["CHERE_INVOKING"] = "1", ["MSYS2_PATH_TYPE"] = "minimal" },
            "MSYS2 MINGW64 gcc");
    }

    internal CompiledCFixture(string name, string[] sources, params string[] linkerArguments)
    {
        directory = Path.Combine(AppContext.BaseDirectory, name + "-evidence");
        executable = new Lazy<string>(() =>
        {
            Directory.CreateDirectory(directory);
            var output = Path.Combine(directory, name + (OperatingSystem.IsWindows() ? ".exe" : ""));
            string[] arguments = ["-std=c11", "-Wall", "-Wextra", "-Werror", "-O0", "-g",
                .. linkerArguments, .. sources.Select(source => Path.Combine(AppContext.BaseDirectory, "Fixtures", source)),
                "-o", output];
            var invocation = BuildCompileInvocation(OperatingSystem.IsWindows(), MsysRoot, LinuxCompiler, arguments);
            if (!File.Exists(invocation.FileName))
                throw new FileNotFoundException("GNU-compatible C toolchain is mandatory (MSYS2 bash on Windows, VICE_NATIVE_CC or /usr/bin/gcc on Linux).", invocation.FileName);
            var result = Execute(invocation, "compile");
            if (result.ExitCode != 0 || !File.Exists(output))
                throw new InvalidOperationException($"C fixture compile failed ({result.ExitCode}): {result.Error}");
            return output;
        });
    }

    /// <summary>Compiled fixture path (compiles on first use); for callers that drive stdin themselves.</summary>
    internal string ExecutablePath => executable.Value;

    /// <summary>Evidence folder holding the compiler and run logs.</summary>
    internal string EvidenceDirectory => directory;

    internal (int ExitCode, string Output, string Error) Run(string[] arguments) =>
        Execute(new Invocation(executable.Value, arguments, new Dictionary<string, string>(), executable.Value), "run-" + Guid.NewGuid().ToString("N"));

    private (int ExitCode, string Output, string Error) Execute(Invocation invocation, string receiptName)
    {
        var start = new ProcessStartInfo(invocation.FileName)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory
        };
        foreach (var argument in invocation.ArgumentList) start.ArgumentList.Add(argument);
        foreach (var (key, value) in invocation.Environment) start.Environment[key] = value;
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
            $"{DateTimeOffset.Now:O}\nToolchain: {invocation.Description}\nExecutable: {invocation.FileName}\nArguments: {string.Join(" | ", invocation.ArgumentList)}\n" +
            $"Environment: {string.Join(" ", invocation.Environment.Select(kv => kv.Key + "=" + kv.Value))}\n" +
            $"ExitCode: {process.ExitCode}\nTimedOut: {timedOut}\nSTDOUT:\n{streams[0]}\nSTDERR:\n{streams[1]}");
        if (timedOut) throw new TimeoutException("C fixture/compiler timed out; this is not a passing gate.");
        return (process.ExitCode, streams[0], streams[1]);
    }
}
