namespace ViceSharp.Xbox.ViewModels;

using System.Collections.Generic;

/// <summary>
/// One physical row of the on-screen keyboard: a named wrapper around the row's keys. The nested
/// per-row {x:Bind} DataTemplate needs an x:DataType, and a bare
/// <c>IReadOnlyList&lt;VirtualKeyEntry&gt;</c> cannot be named in XAML - so the view-model projects its
/// rows through this type. See <c>VirtualKeyboardOverlay.xaml</c>.
/// </summary>
/// <param name="Keys">The keys in this row, left to right.</param>
public sealed record VirtualKeyboardRow(IReadOnlyList<VirtualKeyEntry> Keys);
