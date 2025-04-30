using System.Collections.Concurrent;

namespace Firehorse.Chess;

public class ScraperPositionQueue {
    private readonly (uint, uint)[] positions;
    private ConcurrentStack<(uint, uint)> queue = new();
    private readonly Lock refillLock = new();

    public ScraperPositionQueue() {
        var list = new List<(uint, uint)>();

        const int end = Program.MapSize - Program.HalfSubscriptionSize;
        for (var y = Program.HalfSubscriptionSize; y < end; y += Program.SubscriptionSize) {
            for (var x = Program.HalfSubscriptionSize; x < end; x += Program.SubscriptionSize) {
                list.Add(((uint) x, (uint) y));
            }
        }

        this.positions = list.ToArray();
        this.Refill();
    }

    private void Refill() {
        lock (this.refillLock) {
            if (!this.queue.IsEmpty) return; // someone else holding this lock refilled it
            this.queue = new ConcurrentStack<(uint, uint)>(this.positions);
        }
    }

    public void SubmitWork((uint, uint) position) => this.queue.Push(position);

    public (uint, uint) GetNextPosition() {
        while (true) {
            if (this.queue.TryPop(out var position)) return position;
            this.Refill();
        }
    }
}
