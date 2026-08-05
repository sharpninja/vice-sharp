namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// FEAT-XAOTBIND-001. Reflection <c>{Binding ...}</c> dies under CsWinRT AOT binding
/// mode with no BindingFailed event (FIX-XKBDPANEL-001). The Xbox UWP head therefore
/// uses compiled <c>{x:Bind}</c> only. This gate fails closed if any head XAML
/// reintroduces reflection bindings.
/// </summary>
/// <remarks>
/// FR: FR-XBOXUI (10-foot UI remains populated under Store-AOT path prep).
/// TR: TR-XBOXUI-001 / FIX-XKBDPANEL-001 follow-up.
/// Use case: Release/AOT-safe XAML keeps Settings, keyboard, and menu titles visible.
/// Acceptance: every <c>*.xaml</c> under <c>src/ViceSharp.Xbox</c> contains zero
/// reflection <c>{Binding</c> tokens (comments stripped so XML-doc examples do not
/// false-positive); HomePage and keyboard rows use <c>x:Bind</c> / <c>VirtualKeyRow</c>.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxAotBindingConventionTests
{
    /// <summary>
    /// Matches a reflection markup extension open, e.g. <c>{Binding Title}</c> or
    /// <c>{Binding}</c>. Case-sensitive: XAML uses <c>Binding</c>.
    /// </summary>
    private static readonly Regex ReflectionBindingMarkup = new(
        @"\{Binding(\s|\})",
        RegexOptions.Compiled);

    /// <summary>
    /// FEAT-XAOTBIND-001.
    /// Use case: an agent reintroduces a reflection binding after the AOT-safe migration.
    /// Acceptance: no <c>{Binding</c> remains in the Xbox head XAML tree.
    /// </summary>
    [Fact]
    public void XboxHeadXaml_HasNoReflectionBindings()
    {
        var root = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox");
        Assert.True(Directory.Exists(root), $"Expected Xbox head at '{root}'.");

        var offenders = new System.Collections.Generic.List<string>();
        foreach (var path in Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = StripXmlComments(File.ReadAllText(path));
            if (ReflectionBindingMarkup.IsMatch(text))
            {
                offenders.Add(Path.GetRelativePath(RepoRoot, path).Replace('\\', '/'));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Reflection {{Binding}} is forbidden under FEAT-XAOTBIND-001 (CsWinRT AOT kills it). Offenders: "
            + string.Join(", ", offenders));
    }

    /// <summary>
    /// FEAT-XAOTBIND-001.
    /// Use case: HomePage title and keyboard rows must use compiled binds.
    /// Acceptance: HomePage has <c>x:Bind ViewModel.Title</c>; keyboard row template
    /// uses <c>VirtualKeyRow</c> and <c>x:Bind Keys</c>.
    /// </summary>
    [Fact]
    public void HomeAndKeyboard_UseCompiledBinds_AndVirtualKeyRow()
    {
        var home = StripXmlComments(File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "Views", "HomePage.xaml")));
        Assert.Contains("x:Bind ViewModel.Title", home, StringComparison.Ordinal);
        Assert.DoesNotContain("{Binding", home, StringComparison.Ordinal);

        var keyboard = StripXmlComments(File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "Controls", "VirtualKeyboardOverlay.xaml")));
        Assert.Contains("x:DataType=\"vm:VirtualKeyRow\"", keyboard, StringComparison.Ordinal);
        Assert.Contains("x:Bind Keys", keyboard, StringComparison.Ordinal);
        Assert.DoesNotContain("{Binding", keyboard, StringComparison.Ordinal);

        var rowType = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox.ViewModels", "VirtualKeyRow.cs");
        Assert.True(File.Exists(rowType), "Expected VirtualKeyRow.cs for compiled row binds.");
    }

    private static string StripXmlComments(string text)
    {
        return Regex.Replace(text, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root (ViceSharp.slnx).");

            return directory.FullName;
        }
    }
}
