using System.Net;

namespace Firehorse;

// ReSharper disable AccessToDisposedClosure
public static class Program {
    public const int BoardSize = 8;
    public const int BoardsPerAxis = 1000;
    public const int MapSize = BoardsPerAxis * BoardSize;

    public const int SubscriptionSize = 96;
    public const int HalfSubscriptionSize = SubscriptionSize / 2;

    public static async Task Main() {
        var tasks = new List<Task>();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, _) => {
            Console.WriteLine("cya");
            cts.Cancel();
        };

        var host = IPEndPoint.Parse(Environment.GetEnvironmentVariable("FIREHORSE_HOST") ?? "127.0.0.1:42069");
        var numConnections = int.Parse(Environment.GetEnvironmentVariable("FIREHORSE_NUM_CONNECTIONS") ?? "1");
        Console.WriteLine($"Listening on {host} with {numConnections} connections");

        using var dispatcher = new Dispatcher(host);

        var positions = new List<(int, int)>();
        const int end = MapSize - HalfSubscriptionSize;
        for (var y = HalfSubscriptionSize; y < end; y += SubscriptionSize) {
            for (var x = HalfSubscriptionSize; x < end; x += SubscriptionSize) {
                positions.Add((x, y));
            }
        }

        var chunkCount = (int) Math.Ceiling(positions.Count / (double) numConnections);
        var chunks = positions.Chunk(chunkCount).ToList();
        Console.WriteLine($"{positions.Count} positions, {chunks.Count} chunks (~{chunks[0].Length} per chunk)");

        IWebProxy? proxy = null;
        if (Environment.GetEnvironmentVariable("FIREHORSE_PROXY_URL") is { } url) {
            var proxyUsername = Environment.GetEnvironmentVariable("FIREHORSE_PROXY_USERNAME");
            var proxyPassword = Environment.GetEnvironmentVariable("FIREHORSE_PROXY_PASSWORD");

            var credentials = (proxyUsername is not null || proxyPassword is not null)
                ? new NetworkCredential(proxyUsername, proxyPassword)
                : null;

            Console.WriteLine(credentials != null
                ? $"Using proxy (with auth): {url}"
                : $"Using proxy (without auth): {url}");

            proxy = new WebProxy(url) {
                Credentials = credentials
            };
        } else {
            Console.WriteLine("Not using proxy (be careful!!!)");
        }

#if DEBUG
        Console.WriteLine("Starting in three seconds...");
        await Task.Delay(1000, cts.Token);
#endif

        Console.WriteLine("Creating scrapers...");
        for (var i = 0; i < numConnections; i++) {
            var chunk = chunks[i];
            if (chunk.Length == 0) throw new Exception($"No data in chunk {i}");

            // Console.WriteLine($"Creating scraper {i}");
            var scraper = new Scraper(proxy, i, chunk);

            tasks.Add(Task.Run(async () => {
                try {
                    await scraper.ConnectAsync(cts.Token);
                    await scraper.RunAsync(dispatcher.Dispatch, cts.Token);
                } catch (Exception e) {
                    // poor man's error handling, FIXME reconnect scraper
                    Console.WriteLine(e);
                }
            }, cts.Token));
        }

        Console.WriteLine("Starting dispatcher...");
        tasks.Add(dispatcher.RunAsync(cts.Token));

        Console.WriteLine("Running, glhf");
        await Task.WhenAll(tasks);
    }
}
