using MarkMello.Infrastructure.Platform;

namespace MarkMello.Presentation.Tests;

public sealed class SingleInstanceChannelTests
{
    [Fact]
    public void SecondClaimIsRefusedWhileFirstIsAlive()
    {
        var identity = Guid.NewGuid().ToString();

        using var first = SingleInstanceChannel.TryClaim(identity);
        using var second = SingleInstanceChannel.TryClaim(identity);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task PathSentBySecondInstanceReachesTheFirst()
    {
        var identity = Guid.NewGuid().ToString();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var first = SingleInstanceChannel.TryClaim(identity);
        Assert.NotNull(first);
        first.StartListening(path => received.TrySetResult(path));

        Assert.True(SingleInstanceChannel.TrySend(@"C:\docs\отчёт.md", identity));

        Assert.Equal(@"C:\docs\отчёт.md", await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task SeveralPathsInARowAreAllDelivered()
    {
        var identity = Guid.NewGuid().ToString();
        var received = new List<string>();
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var first = SingleInstanceChannel.TryClaim(identity);
        Assert.NotNull(first);
        first.StartListening(path =>
        {
            lock (received)
            {
                received.Add(path);
                if (received.Count == 3)
                {
                    done.TrySetResult();
                }
            }
        });

        foreach (var name in new[] { "a.md", "b.md", "c.md" })
        {
            Assert.True(SingleInstanceChannel.TrySend(name, identity));
        }

        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        lock (received)
        {
            Assert.Equal(["a.md", "b.md", "c.md"], received.Order().ToArray());
        }
    }
}
