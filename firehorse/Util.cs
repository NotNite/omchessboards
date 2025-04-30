namespace Firehorse;

public class Util {
    // https://stackoverflow.com/a/79386570
    public static Task WrapTasks(
        CancellationTokenSource cts,
        params IEnumerable<Task> tasks
    ) => Task.WhenAll(tasks.Select(task => {
        return task.ContinueWith(t => {
            if (t.IsFaulted) cts.Cancel();
            return t;
        }).Unwrap();
    }));
}
