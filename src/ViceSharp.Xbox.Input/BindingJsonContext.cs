namespace ViceSharp.Xbox.Input;

using System.Text.Json.Serialization;

/// <summary>
/// The System.Text.Json SOURCE-GENERATED serialization context for the remappable
/// binding-persistence model (PLAN-XBOXUWP S12, IMPL-XBOXUWP-012). It is the ONE
/// and ONLY serialization path for a <see cref="BindingProfile"/>: callers pass its
/// generated <c>JsonTypeInfo</c> (<see cref="JsonSerializerContext.GetTypeInfo"/> /
/// the strongly-typed <c>BindingProfile</c> property) into the JsonSerializer
/// serialize/deserialize methods, never a reflective generic overload.
/// </summary>
/// <remarks>
/// <para>
/// Being source-generated makes the persistence path AOT-safe and reflection-free:
/// the generator emits the metadata at compile time, so the library links clean
/// under Native AOT / IL trimming (<c>IsAotCompatible</c> +
/// <c>EnableTrimAnalyzer</c>) inside the UWP AppContainer. System.Text.Json is part
/// of the net10.0 shared framework and the <see cref="JsonSerializerContext"/>
/// generator is bundled with the SDK, so no package reference is required.
/// </para>
/// <para>
/// <b>Enum serialization: integer values (the AOT-safe default).</b> The bindable
/// enums (<see cref="BindableInput"/>, <see cref="AppCommand"/>,
/// <see cref="BindingActivation"/>) serialize as their underlying integer. This is
/// deliberate: it is the reflection-free default (the source-gen-compatible string
/// path would need a <c>JsonStringEnumConverter&lt;T&gt;</c> per enum), and the file
/// is versioned (<c>bindings.v1.json</c>), so any future enum-shape change rides a
/// schema-version bump rather than relying on member-name stability.
/// </para>
/// </remarks>
[JsonSerializable(typeof(BindingProfile))]
[JsonSerializable(typeof(ButtonBinding))]
public sealed partial class BindingJsonContext : JsonSerializerContext
{
}
