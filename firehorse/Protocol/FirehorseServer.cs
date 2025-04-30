using System.Net;
using System.Net.Sockets;
using Capnp;
using Capnp.Rpc;
using ZstdSharp;
using Exception = System.Exception;

namespace Firehorse.Protocol;

public class FirehorseServer(IPEndPoint endpoint, FirehorseRpc rpc) : IDisposable {
    private readonly TcpListener listener = new(endpoint);
    private readonly RpcEngine engine = new() {
        Main = rpc
    };

    public async Task RunAsync(CancellationToken cancellationToken = default) {
        this.listener.Start();

        while (!cancellationToken.IsCancellationRequested) {
            var client = await this.listener.AcceptTcpClientAsync(cancellationToken);
            _ = Task.Run(async () => {
                try {
                    await this.HandleClientAsync(client, cancellationToken);
                } catch (Exception) {
                    // Console.WriteLine(e);
                }
            }, cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken = default) {
        await using var stream = client.GetStream();
        await using var decompress = new DecompressionStream(stream);
        await using var compress = new CompressionStream(stream);

        using var pump = new FramePump(compress);
        var endpoint = new FirehorseEndpoint(pump);
        var rpcEndpoint = this.engine.AddEndpoint(endpoint);

        while (!cancellationToken.IsCancellationRequested) {
            var frame = Framing.ReadSegments(decompress);
            rpcEndpoint.Forward(frame);
        }
    }

    public void Dispose() {
        this.listener.Dispose();
        GC.SuppressFinalize(this);
    }
}
