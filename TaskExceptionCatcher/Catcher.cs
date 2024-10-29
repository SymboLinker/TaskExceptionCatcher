namespace TaskExceptionCatcher;

public static class Catcher
{
    public static async Task<CatchResult<TValue>> Run<TValue>(Func<Task<TValue>> taskFunc)
    {
        try
        {
            var task = taskFunc();
            await task.ConfigureAwait(false);
            return new CatchResult<TValue> { Value = task.Result };
        }
        catch (Exception ex)
        {
            return new CatchResult<TValue> { Exception = ex };
        }
    }

    public static Task<CatchResult<TValue>> Run<TValue>(Func<TValue> func, CancellationToken cancellationToken = default)
        => Run(() => Task.Run(func, cancellationToken));

    public class CatchResult<TValue>
    {
        public TValue? Value { get; set; }
        public Exception? Exception { get; set; }
    }
}
