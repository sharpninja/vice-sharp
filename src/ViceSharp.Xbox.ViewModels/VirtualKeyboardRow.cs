namespace ViceSharp.Xbox.ViewModels;

using System.Collections.Generic;

/// <summary>
/// One physical row of the on-screen keyboard: a named wrapper around the row's keys. The nested
/// per-row {x:Bind} DataTemplate needs an x:DataType, and a bare
/// <c>IReadOnlyList&lt;VirtualKeyEntry&gt;</c> cannot be named in XAML - so the view-model projects its
/// rows through this type. See <c>VirtualKeyboardOverlay.xaml</c>.
/// </summary>
/// <param name="Keys">The keys in this row, left to right.</param>
/// <param name="Centered">
/// FEAT-XKEYCAPMODEL-001 (operator 2026-07-18): <c>true</c> for the space-bar row, which the
/// real machine centres under the alphanumeric block rather than left-aligning with the
/// staggered rows above. The view binds the row's horizontal alignment to this.
/// </param>
public sealed record VirtualKeyboardRow(IReadOnlyList<VirtualKeyEntry> Keys, bool Centered = false);
