using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using ZstdSharp;

namespace Firehorse;

public class Dispatcher(IPEndPoint endpoint) : IDisposable {
    private readonly TcpListener listener = new(endpoint);
    private readonly List<ChannelWriter<ReadOnlyMemory<byte>>> channels = [];

    public async Task RunAsync(CancellationToken cancellationToken = default) {
        this.listener.Start();

        var tasks = new List<Task>();

        while (!cancellationToken.IsCancellationRequested) {
            var client = await this.listener.AcceptTcpClientAsync(cancellationToken);

            var task = Task.Run(async () => {
                var channel = Channel.CreateBounded<ReadOnlyMemory<byte>>(new BoundedChannelOptions(1) {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.DropWrite
                });
                var reader = channel.Reader;
                var writer = channel.Writer;

                lock (this.channels) this.channels.Add(writer);

                await using var stream = client.GetStream();
                await using var zstd = new CompressionStream(stream);

                await foreach (var data in reader.ReadAllAsync(cancellationToken)) {
                    try {
                        await zstd.WriteAsync(data, cancellationToken);
                        //await stream.WriteAsync(data, cancellationToken);
                    } catch {
                        // ignored
                        break;
                    }
                }

                lock (this.channels) this.channels.Remove(writer);

                client.Dispose();
            }, cancellationToken);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public void Dispatch(ReadOnlyMemory<byte> data) {
        // Lock contention isn't real and can't hurt you (FIXME maybe)
        lock (this.channels) this.channels.ForEach(channel => channel.TryWrite(data));
    }

    public void Dispose() {
        this.listener.Dispose();
        GC.SuppressFinalize(this);
    }
}
