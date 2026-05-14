using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ViceSharp.TestHarness;

[CollectionDefinition("NativeVice", DisableParallelization = true)]
public sealed class NativeViceCollection
{
}
