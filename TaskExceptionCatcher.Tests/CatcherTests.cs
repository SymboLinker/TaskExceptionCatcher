using AwaitMultiple;

namespace TaskExceptionCatcher.Tests;

public class CatcherTests
{
    static Task<int> GetIntAsync(int i) => Task.FromResult(i);
    static Task<int> FailIntAsync(int i) => throw new Exception($"Could not get int {i}.");
    static async Task<int> WaitEndlesslyForIntAsync(int i, CancellationToken cancellationToken)
    {
        await Task.Delay(-1, cancellationToken);
        return i;
    }

    [Fact]
    public async Task TestWithCancellationException()
    {
        var cancellationToken = new CancellationToken(canceled: true);
        var result = await Catcher.Run(() => WaitEndlesslyForIntAsync(2, cancellationToken));
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task TestWithException()
    {
        var result = await Catcher.Run(() => FailIntAsync(2));
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task TestNoException()
    {
        var result = await Catcher.Run(() => GetIntAsync(2));
        Assert.Null(result.Exception);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task TestAwaitMultipleWithExceptions()
    {
        var cancellationToken = new CancellationToken(canceled: true);

        var (output1, output2, output3, output4) = await Tasks(
            GetIntAsync(1),
            Catcher.Run(() => FailIntAsync(2)),
            Catcher.Run(() => WaitEndlesslyForIntAsync(3, cancellationToken)),
            GetIntAsync(4));

        Assert.Equal(1, output1);
        Assert.Equal(4, output4);

        if (output2.Exception is { } exception)
        {
            Assert.Equal("Could not get int 2.", exception.Message);
        }
        else
        {
            Assert.Fail();
        }

        Assert.NotNull(output3.Exception);
        Assert.IsType<TaskCanceledException>(output3.Exception);
    }

    [Fact]
    public async Task TestAwaitMultipleNoExceptions()
    {
        var cancellationToken = new CancellationToken(canceled: true);

        var (output1, output2, output3, output4) = await Tasks(
            GetIntAsync(1),
            Catcher.Run(() => GetIntAsync(2)),
            Catcher.Run(() => GetIntAsync(3)),
            GetIntAsync(4));

        Assert.Equal(1, output1);
        Assert.Equal(4, output4);

        Assert.Null(output2.Exception);
        Assert.Equal(2, output2.Value);

        Assert.Null(output3.Exception);
        Assert.Equal(3, output3.Value);
    }

    [Fact]
    public async Task TestAwaitMultipleCancellationExceptions()
    {
        var cancellationToken = new CancellationToken(canceled: true);

        var (output1, output2, output3, output4) = await Tasks(
            GetIntAsync(1),
            Catcher.Run(() => WaitEndlesslyForIntAsync(2, cancellationToken)),
            Catcher.Run(() => WaitEndlesslyForIntAsync(3, cancellationToken)),
            GetIntAsync(4));

        Assert.Equal(1, output1);
        Assert.NotNull(output2.Exception);
        Assert.NotNull(output3.Exception);
        Assert.Equal(4, output4);
    }

    [Fact]
    public async Task TestAwaitMultipleExceptionsAndMultipleCaught()
    {
        var cancellationToken = new CancellationToken(canceled: true);

        try
        {
            var (output1, output2, output3, output4) = await Tasks(
                Task.Run(() => FailIntAsync(1)),
                Catcher.Run(() => WaitEndlesslyForIntAsync(2, cancellationToken)),
                Catcher.Run(() => WaitEndlesslyForIntAsync(3, cancellationToken)),
                Task.Run(() => FailIntAsync(4)),
                exceptionOption: ExceptionOption.Aggregate);
        }
        catch (Exception ex)
        {
            var aggregateException = Assert.IsType<AggregateException>(ex);
            Assert.Equal(2, aggregateException.InnerExceptions.Count);
            var messages = aggregateException.InnerExceptions.Select(x => x.Message);
            Assert.Contains("Could not get int 1.", messages);
            Assert.Contains("Could not get int 4.", messages);
            return;
        }

        Assert.Fail();
    }

    [Fact]
    public async Task TestSynchronousFunctionsDoNotThrow()
    {
        int SynchronouslyReturn(int value) { return value; }
        int SynchronouslyThrow(int value) { throw new Exception($"Task {value} threw."); }

        var (output1, output2, output3) = await Tasks(
            Task.Run(() => SynchronouslyReturn(1)),
            Catcher.Run(() => SynchronouslyReturn(2)),
            Catcher.Run(() => SynchronouslyThrow(3))
            );

        Assert.Equal(1, output1);

        Assert.Null(output2.Exception);
        Assert.Equal(2, output2.Value);

        Assert.NotNull(output3.Exception);
        Assert.Equal("Task 3 threw.", output3.Exception.Message);
    }
}
