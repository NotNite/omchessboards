using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Capnp;
using CapnpGen;
using ZstdSharp;
using Exception = System.Exception;

namespace Firehorse;

// ReSharper disable AccessToDisposedClosure
/// <summary>Broadcasts snapshots to all connected clients, as well as forwarding commands to scrapers.</summary>
public class Dispatcher(IPEndPoint endpoint, ChannelWriter<Command.READER> sender) : IDisposable {
    private readonly TcpListener listener = new(endpoint);
    private readonly List<ChannelWriter<Snapshot>> clients = [];

    public async Task RunAsync(CancellationToken cancellationToken = default) {
        this.listener.Start();

        while (!cancellationToken.IsCancellationRequested) {
            var client = await this.listener.AcceptTcpClientAsync(cancellationToken);
            _ = Task.Run(() => this.HandleClient(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken cancellationToken = default) {
        await using var stream = client.GetStream();
        await using var zstd = new CompressionStream(stream);
        using var pump = new FramePump(zstd);

        var channel = Channel.CreateUnbounded<Snapshot>();
        lock (this.clients) this.clients.Add(channel.Writer);

        var sendTask = Task.Run(async () => {
            await foreach (var data in channel.Reader.ReadAllAsync(cancellationToken)) {
                try {
                    // this library is so bleh :(
                    var msg = MessageBuilder.Create();
                    var writer = msg.BuildRoot<Snapshot.WRITER>();
                    data.serialize(writer);
                    pump.Send(msg.Frame);
                } catch {
                    // ignored, probably closed
                    break;
                }
            }
        }, cancellationToken);

        var recvTask = Task.Run(() => {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    var frame = Framing.ReadSegments(stream);
                    var deserializer = DeserializerState.CreateRoot(frame);
                    var reader = new Command.READER(deserializer);
                    sender.TryWrite(reader);
                } catch {
                    // ignored, probably closed
                    break;
                }
            }
        }, cancellationToken);

        try {
            Task[] tasks = [sendTask, recvTask];
            await Task.WhenAll(tasks);
        } catch (Exception e) {
            // Console.WriteLine(e);
        }

        lock (this.clients) this.clients.Remove(channel.Writer);
        client.Dispose();
    }

    public void Dispatch(Snapshot data) {
        // Lock contention isn't real and can't hurt you (FIXME maybe)
        lock (this.clients) {
            foreach (var client in this.clients) {
                client.TryWrite(data);
            }
        }
    }

    public void Dispose() {
        this.listener.Dispose();
        GC.SuppressFinalize(this);
    }
}
