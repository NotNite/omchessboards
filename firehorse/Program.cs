using System.Collections.Concurrent;
using System.Net;
using System.Threading.Channels;
using CapnpGen;

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

        var commandChannel = Channel.CreateUnbounded<Command.READER>();
        using var dispatcher = new Dispatcher(host, commandChannel);

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
        await Task.Delay(3000, cts.Token);
#endif

        Console.WriteLine("Creating scrapers...");
        var queue = new PositionQueue();
        for (var i = 0; i < numConnections; i++) {
            tasks.Add(Task.Run(async () => {
                while (!cts.Token.IsCancellationRequested) {
                    try {
                        using var scraper = new Scraper(proxy, queue, dispatcher.Dispatch, commandChannel.Reader);
                        await scraper.ConnectAsync(cts.Token);
                        await scraper.RunAsync(cts.Token);
                        await scraper.DisconnectAsync(cts.Token);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }
            }, cts.Token));
        }

        Console.WriteLine("Starting dispatcher...");
        tasks.Add(dispatcher.RunAsync(cts.Token));

        Console.WriteLine("Running, glhf");
        await Task.WhenAll(tasks);
    }
}
