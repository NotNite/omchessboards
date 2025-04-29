using System.Net;

namespace Firehorse;

public static class Program {
    public const int BoardSize = 8;
    public const int BoardsPerAxis = 1000;
    public const int MapSize = BoardsPerAxis * BoardSize;

    public const int SubscriptionSize = 96;
    public const int HalfSubscriptionSize = SubscriptionSize / 2;

    public const int NumProxies = 1;      // our limit
    public const int MaxConnections = 15; // server limit
    public const int TotalConnections = NumProxies * MaxConnections;

    public static void Main() {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, _) => {
            Console.WriteLine("cya");
            cts.Cancel();
        };

        Console.WriteLine($"{NumProxies} proxies, {MaxConnections} connections ({TotalConnections} total)");

        var tasks = new List<Task>();

        using var dispatcher = new Dispatcher(new IPEndPoint(IPAddress.Loopback, 42069));
        tasks.Add(dispatcher.RunAsync(cts.Token));

        var positions = new List<(int, int)>();
        const int end = MapSize - HalfSubscriptionSize;
        for (var y = HalfSubscriptionSize; y < end; y += SubscriptionSize) {
            for (var x = HalfSubscriptionSize; x < end; x += SubscriptionSize) {
                positions.Add((x, y));
            }
        }

        var chunks = positions.Chunk(positions.Count / TotalConnections).ToList();
        for (var proxyId = 0; proxyId < NumProxies; proxyId++) {
            var username = $"13376969{proxyId:X4}";

            for (var i = 0; i < MaxConnections; i++) {
                var chunkIdx = (proxyId * MaxConnections) + i;
                var chunk = chunks[chunkIdx];
                if (chunk.Length == 0) throw new Exception($"No data in chunk {chunkIdx}");

                var scraper = new Scraper(username, chunk);
                tasks.Add(scraper.RunAsync(async (data) => {
                    // ReSharper disable once AccessToDisposedClosure
                    await dispatcher.DispatchAsync(data, cts.Token);
                }, cts.Token));
            }
        }

        Task.WaitAll(tasks, cts.Token);
    }
}
