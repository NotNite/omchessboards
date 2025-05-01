using System.Threading.Channels;

namespace Firehorse.Protocol;

// ReSharper disable InconsistentlySynchronizedField
public class ChannelRelay<T>(Channel<T> channel) {
    public ChannelWriter<T> Writer => channel.Writer;

    private readonly List<ChannelWriter<T>> writers = [];

    public ChannelRelay() : this(Channel.CreateUnbounded<T>()) { }

    public DisposableReader<T> CreateReader() {
        return new DisposableReader<T>(this.writers);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default) {
        await foreach (var data in channel.Reader.ReadAllAsync(cancellationToken)) {
            lock (this.writers) {
                foreach (var writer in this.writers) {
                    writer.TryWrite(data);
                }
            }
        }
    }
}

public class DisposableReader<T> : IDisposable {
    private readonly Channel<T> channel;
    private readonly List<ChannelWriter<T>> writers;

    public DisposableReader(List<ChannelWriter<T>> writers) {
        this.channel = Channel.CreateUnbounded<T>();
        this.writers = writers;
        lock (this.writers) this.writers.Add(this.channel.Writer);
    }

    public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default) {
        return this.channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Dispose() {
        lock (this.writers) this.writers.Remove(this.channel.Writer);
        this.channel.Writer.Complete();
        GC.SuppressFinalize(this);
    }
}
