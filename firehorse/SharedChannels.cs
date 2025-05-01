using CapnpGen;
using Chess;
using Firehorse.Chess;
using Firehorse.Protocol;

namespace Firehorse;

public class SharedChannels {
    public readonly ChannelRelay<Snapshot> SnapshotRelay = new();
    public readonly ChannelRelay<IReadOnlyList<RemoteMove>> MoveRelay = new();
    public readonly ChannelRelay<IReadOnlyList<MoveResult>> CaptureRelay = new();
    public readonly ChannelRelay<IReadOnlyList<uint>> AdoptRelay = new();

    public readonly ConstantScraperQueue<(uint, uint)> PositionQueue = new(CreatePositions());
    public readonly AsyncScraperQueue<ClientMove, ServerValidMove> WhiteMoveQueue = new();
    public readonly AsyncScraperQueue<ClientMove, ServerValidMove> BlackMoveQueue = new();

    public async Task RunAllRelays(CancellationToken cancellationToken = default) {
        using var relayCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(relayCts.Token, cancellationToken);

        await Util.WrapTasks(
            relayCts,
            this.SnapshotRelay.RunAsync(linked.Token),
            this.MoveRelay.RunAsync(linked.Token),
            this.CaptureRelay.RunAsync(linked.Token),
            this.AdoptRelay.RunAsync(linked.Token)
        );
    }

    private static List<(uint, uint)> CreatePositions() {
        const int end = Program.MapSize - Program.HalfSubscriptionSize;
        const int duplicate = 3; // add the work a few times to prevent constant refills

        var positionList = new List<(uint, uint)>();
        for (var i = 0; i < duplicate; i++) {
            for (var y = Program.HalfSubscriptionSize; y < end; y += Program.SubscriptionSize) {
                for (var x = Program.HalfSubscriptionSize; x < end; x += Program.SubscriptionSize) {
                    positionList.Add(((uint) x, (uint) y));
                }
            }
        }

        return positionList;
    }
}
