using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class SingleFlightGateTests
{
    [Fact]
    public async Task RunAsync_SkipsOverlappingCall()
    {
        using var gate = new SingleFlightGate();
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var runs = 0;

        var first = gate.RunAsync(async () =>
        {
            Interlocked.Increment(ref runs);
            started.SetResult();
            await release.Task;
        });

        await started.Task;
        var secondRan = await gate.RunAsync(() =>
        {
            Interlocked.Increment(ref runs);
            return Task.CompletedTask;
        });

        Assert.False(secondRan);
        release.SetResult();
        Assert.True(await first);
        Assert.Equal(1, runs);

        var thirdRan = await gate.RunAsync(() =>
        {
            Interlocked.Increment(ref runs);
            return Task.CompletedTask;
        });

        Assert.True(thirdRan);
        Assert.Equal(2, runs);
    }

    [Fact]
    public async Task RunAsync_ReleasesGateOnException()
    {
        using var gate = new SingleFlightGate();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gate.RunAsync(() => throw new InvalidOperationException("boom")));

        var ran = false;
        Assert.True(await gate.RunAsync(() =>
        {
            ran = true;
            return Task.CompletedTask;
        }));
        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_DisposeDuringRun_DoesNotThrowOnRelease()
    {
        var gate = new SingleFlightGate();
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();

        var first = gate.RunAsync(async () =>
        {
            started.SetResult();
            await release.Task;
        });

        await started.Task;
        gate.Dispose();
        release.SetResult();

        Assert.True(await first);
    }
}
