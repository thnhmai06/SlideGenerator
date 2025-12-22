using SlideGenerator.Domain.Job.Components;

namespace SlideGenerator.Tests.Domain;

[TestClass]
public sealed class PauseSignalTests
{
    [TestMethod]
    public async Task WaitIfPausedAsync_BlocksUntilResume()
    {
        var signal = new PauseSignal();
        signal.Pause();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var waitTask = signal.WaitIfPausedAsync(cts.Token);

        Assert.IsFalse(waitTask.IsCompleted);

        signal.Resume();
        await waitTask;

        Assert.IsTrue(waitTask.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task WaitIfPausedAsync_ReturnsImmediatelyWhenNotPaused()
    {
        var signal = new PauseSignal();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await signal.WaitIfPausedAsync(cts.Token);
    }
}