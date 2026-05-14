using ViceSharp.Protocol;

namespace ViceSharp.Host.Runtime;

public interface IEmulatorRuntimeFactory
{
    EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request);
}
