using System.Runtime.CompilerServices;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ViceFactAttribute : FactAttribute
{
    public ViceFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!ViceNative.IsAvailable)
            Skip = ViceNative.AvailabilityMessage;
    }
}
