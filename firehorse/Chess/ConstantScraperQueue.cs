using System.Collections.Concurrent;

namespace Firehorse.Chess;

/// <summary>A scraper queue that constantly refills itself.</summary>
public class ConstantScraperQueue<T>(IEnumerable<T> initial) {
    private ConcurrentStack<T> queue = new(initial);
    private readonly Lock refillLock = new();

    private void Refill() {
        lock (this.refillLock) {
            if (!this.queue.IsEmpty) return; // someone else holding this lock refilled it
            this.queue = new ConcurrentStack<T>(initial);
        }
    }

    public T Get() {
        while (true) {
            if (this.queue.TryPop(out var position)) return position;
            this.Refill();
        }
    }

    public void Submit(T item) => this.queue.Push(item);
}
