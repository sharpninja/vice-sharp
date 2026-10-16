// FEAT-XMENUSNAP-001: PORTABLE (no #if HAS_UWP) source-generated JSON context for the
// menu's snapshot slot. Source generation keeps the Release-UWP Native-AOT posture
// clean: the reflection JsonSerializer overloads trip the repo-wide trim/AOT analyzers.
namespace ViceSharp.Xbox.Platform;

using System.Text.Json.Serialization;
using ViceSharp.Protocol;

/// <summary>
/// The AOT/trim-safe serializer context for persisting <see cref="SnapshotDto"/>
/// (Format + Cycle + base64 Payload) to the menu's LocalState snapshot slot.
/// </summary>
[JsonSerializable(typeof(SnapshotDto))]
public sealed partial class SnapshotJsonContext : JsonSerializerContext
{
}
