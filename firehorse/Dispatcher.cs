using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Firehorse;

public class Dispatcher(IPEndPoint endpoint) : IDisposable {
    private readonly TcpListener listener = new(endpoint);
    private readonly List<Connection> clients = [];

    public async Task RunAsync(CancellationToken cancellationToken = default) {
        this.listener.Start();

        while (!cancellationToken.IsCancellationRequested) {
            var client = await this.listener.AcceptTcpClientAsync(cancellationToken);
            this.clients.Add(new Connection(client));
        }
    }

    public async Task DispatchAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) {
        // FIXME this also sucks
        foreach (var client in this.clients.ToList()) {
            try {
                await client.Stream.WriteAsync(data, cancellationToken);
            } catch {
                client.Dispose();
                this.clients.Remove(client);
            }
        }
    }

    public void Dispose() {
        foreach (var client in this.clients) client.Dispose();
        this.clients.Clear();
        this.listener.Dispose();
        GC.SuppressFinalize(this);
    }

    public record Connection : IDisposable {
        public readonly TcpClient Client;
        public readonly NetworkStream Stream;

        public Connection(TcpClient client) {
            this.Client = client;
            this.Stream = this.Client.GetStream();
        }

        public void Dispose() {
            this.Stream.Dispose();
            this.Client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
