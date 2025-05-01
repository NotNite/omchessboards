using System.Net;
using Firehorse.Chess;
using Firehorse.Protocol;

namespace Firehorse;

// ReSharper disable AccessToDisposedClosure
public static class Program {
    public const int BoardSize = 8;
    public const int BoardsPerAxis = 1000;
    public const int MapSize = BoardsPerAxis * BoardSize;

    public const int SubscriptionSize = 96;
    public const int HalfSubscriptionSize = SubscriptionSize / 2;

    public static async Task Main() {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, _) => {
            Console.WriteLine("cya");
            cts.Cancel();
        };

        var tasks = new List<Task>();

        var channels = new SharedChannels();
        tasks.Add(Task.Run(() => channels.RunAllRelays(cts.Token), cts.Token));

        using var rpc = new FirehorseRpc(channels);

        var host = IPEndPoint.Parse(Environment.GetEnvironmentVariable("FIREHORSE_HOST") ?? "127.0.0.1:42069");
        using var server = new FirehorseServer(host, rpc);
        Console.WriteLine($"Listening on {host}");

        IWebProxy? proxy = null;
        if (Environment.GetEnvironmentVariable("FIREHORSE_PROXY_URL") is { } url) {
            var proxyUsername = Environment.GetEnvironmentVariable("FIREHORSE_PROXY_USERNAME");
            var proxyPassword = Environment.GetEnvironmentVariable("FIREHORSE_PROXY_PASSWORD");

            var credentials = proxyUsername is not null || proxyPassword is not null
                ? new NetworkCredential(proxyUsername, proxyPassword)
                : null;

            Console.WriteLine(credentials != null
                ? $"Using proxy (with auth): {url}"
                : $"Using proxy (without auth): {url}");

            // TODO: multiple proxies maybe
            proxy = new WebProxy(url) {
                Credentials = credentials
            };
        } else {
            Console.WriteLine("Not using proxy (be careful!!!)");
        }

        var numConnections = int.Parse(Environment.GetEnvironmentVariable("FIREHORSE_NUM_CONNECTIONS") ?? "1");
        Console.WriteLine($"Using {numConnections} connections");

#if DEBUG
        Console.WriteLine("Starting in three seconds...");
        await Task.Delay(3000, cts.Token);
#endif

        Console.WriteLine("Creating scrapers...");
        for (var i = 0; i < numConnections; i++) {
            var id = i;
            // TODO: add the option to create connections one by one when not using a proxy
            tasks.Add(Task.Run(async () => {
                while (!cts.Token.IsCancellationRequested) {
                    using var connection = new Connection(proxy, channels);
                    try {
                        using var scraper = new Scraper(connection, channels);

                        // TODO: go back to using initial state in scraper maybe?
                        await connection.ConnectAsync(isWhite: id % 2 == 0, cancellationToken: cts.Token);

                        using var scraperCts = new CancellationTokenSource();
                        using var linked =
                            CancellationTokenSource.CreateLinkedTokenSource(scraperCts.Token, cts.Token);
                        await Util.WrapTasks(
                            scraperCts,
                            Task.Run(() => connection.RunAsync(linked.Token), linked.Token),
                            Task.Run(() => scraper.RunSnapshotsAsync(linked.Token), linked.Token),
                            Task.Run(() => scraper.RunMovesAsync(linked.Token), linked.Token)
                        );
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }
            }, cts.Token));
        }

        Console.WriteLine("Starting dispatcher...");
        tasks.Add(server.RunAsync(cts.Token));

        Console.WriteLine("Running, glhf");
        await Util.WrapTasks(cts, tasks);
    }
}
