using System.Threading.Channels;

namespace Firehorse.Chess;

/// <summary>A scraper queue that constantly refills itself.</summary>
public class AsyncScraperQueue<T, TR> {
    private readonly Channel<(T, TaskCompletionSource<TR>)> channel =
        Channel.CreateUnbounded<(T, TaskCompletionSource<TR>)>();

    public async Task<(T, TaskCompletionSource<TR>)> GetAsync(CancellationToken cancellationToken = default) {
        while (true) {
            cancellationToken.ThrowIfCancellationRequested();

            var (data, tcs) = await this.channel.Reader.ReadAsync(cancellationToken);
            if (tcs.Task.IsCompleted) continue; // cancelled

            return (data, tcs);
        }
    }

    public void Submit((T, TaskCompletionSource<TR>) data) => this.channel.Writer.TryWrite(data);

    public Task<TR> SubmitAndWait(T data) {
        var tcs = new TaskCompletionSource<TR>();
        this.Submit((data, tcs));
        return tcs.Task;
    }
}
