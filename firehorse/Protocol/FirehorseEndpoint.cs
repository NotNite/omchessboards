using Capnp;
using Capnp.Rpc;

namespace Firehorse.Protocol;

public class FirehorseEndpoint(FramePump pump) : IEndpoint {
    public void Forward(WireFrame frame) => pump.Send(frame);
    public void Flush() => pump.Flush();
    public void Dismiss() => pump.Dispose();
}
