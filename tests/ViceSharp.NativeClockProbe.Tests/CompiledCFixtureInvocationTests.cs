namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Payton directive 2026-10-20T08:07 ("You should always use msys to compile native VICE code"), extended to the
/// test fixtures at 08:14 ("Fixtures too"). On Windows every fixture compile runs inside the MSYS2 MINGW64 login
/// shell, with the compiler arguments passed through "$@" unchanged and the resolved compiler recorded; on Linux
/// the system compiler runs directly. Pure construction of the process invocation, tested on both branches.
/// </summary>
public sealed class CompiledCFixtureInvocationTests
{
    private static readonly string[] Arguments = ["-std=c11", "-Wall", "-c", @"C:\work\a b\x.c", "-o", @"C:\work\a b\x.o"];

    [Fact]
    public void Windows_compiles_inside_the_msys2_mingw64_login_shell_and_passes_arguments_unchanged()
    {
        var inv = CompiledCFixture.BuildCompileInvocation(isWindows: true, msysRoot: @"C:\msys64", linuxCompiler: "/usr/bin/gcc", Arguments);
        Assert.Equal(@"C:\msys64\usr\bin\bash.exe", inv.FileName);
        Assert.Equal("-lc", inv.ArgumentList[0]);
        Assert.Contains("command -v gcc", inv.ArgumentList[1]);
        Assert.Contains("MSYSTEM", inv.ArgumentList[1]);
        Assert.EndsWith("exec gcc \"$@\"", inv.ArgumentList[1]);
        Assert.Equal("bash", inv.ArgumentList[2]);
        Assert.Equal(Arguments, inv.ArgumentList.Skip(3));
        Assert.Equal("MINGW64", inv.Environment["MSYSTEM"]);
        Assert.Equal("1", inv.Environment["CHERE_INVOKING"]);
        Assert.Equal("MSYS2 MINGW64 gcc", inv.Description);
    }

    [Fact]
    public void Linux_runs_the_system_compiler_directly()
    {
        var inv = CompiledCFixture.BuildCompileInvocation(isWindows: false, msysRoot: @"C:\msys64", linuxCompiler: "/usr/bin/gcc", Arguments);
        Assert.Equal("/usr/bin/gcc", inv.FileName);
        Assert.Equal(Arguments, inv.ArgumentList);
        Assert.Empty(inv.Environment);
        Assert.Equal("/usr/bin/gcc", inv.Description);
    }

    [Fact]
    public void Wrong_shapes_are_distinguishable()
    {
        var inv = CompiledCFixture.BuildCompileInvocation(isWindows: true, msysRoot: @"C:\msys64", linuxCompiler: "/usr/bin/gcc", Arguments);
        Assert.NotEqual(@"C:\msys64\mingw64\bin\gcc.exe", inv.FileName);
        Assert.DoesNotContain(inv.ArgumentList, a => a.Contains("mingw64/bin/gcc", StringComparison.Ordinal));
        Assert.NotEqual("-c", inv.ArgumentList[0]);
    }
}
