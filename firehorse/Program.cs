using System.Net;

namespace Firehorse;

// ReSharper disable AccessToDisposedClosure
public static class Program {
    public const int BoardSize = 8;
    public const int BoardsPerAxis = 1000;
    public const int MapSize = BoardsPerAxis * BoardSize;

    public const int SubscriptionSize = 96;
    public const int HalfSubscriptionSize = SubscriptionSize / 2;

    public const int NumProxies = 3;      // cloudflare limit
    public const int MaxConnections = 15; // server(?) limit
    public const int TotalConnections = NumProxies * MaxConnections;

    public static async Task Main() {
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
        Console.WriteLine($"{positions.Count} positions, {chunks.Count} chunks");

        for (var proxyId = 0; proxyId < NumProxies; proxyId++) {
            // TODO: configure this
            var username = $"{proxyId:X4}13376969";
            var proxy = new WebProxy("socks5://rose.host.katie.cat:1234") {
                Credentials = new NetworkCredential(username, "meow")
            };

            for (var i = 0; i < MaxConnections; i++) {
                var chunkIdx = (proxyId * MaxConnections) + i;
                var chunk = chunks[chunkIdx];
                if (chunk.Length == 0) throw new Exception($"No data in chunk {chunkIdx}");

                // NOTE: seems to be a hard limit of 45 per IPv6 /48. must also connect sequentially to avoid 429s
                Console.WriteLine($"Connecting scraper {chunkIdx}");
                var scraper = new Scraper(proxy, chunkIdx, chunk);
                await scraper.ConnectAsync(cts.Token);

                tasks.Add(Task.Run(async () => {
                    try {
                        await scraper.RunAsync(dispatcher.Dispatch, cts.Token);
                    } catch (Exception e) {
                        // poor man's error handling
                        Console.WriteLine(e);
                    }
                }, cts.Token));
            }
        }

        Console.WriteLine("All scrapers connected, running");
        await Task.WhenAll(tasks);
    }
}
