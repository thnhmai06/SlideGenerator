using SlideGenerator.Domain.Features.Jobs.Components;

namespace SlideGenerator.Tests.Domain;

[TestClass]
public sealed class PauseSignalTests
{
    [TestMethod]
    public async Task WaitIfPausedAsync_WhenPaused_ThrowsOperationCanceled()
    {
        var signal = new PauseSignal();
        signal.Pause();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await signal.WaitIfPausedAsync(cts.Token);
            Assert.Fail("Expected OperationCanceledException when paused.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestMethod]
    public async Task WaitIfPausedAsync_ReturnsImmediatelyWhenNotPaused()
    {
        var signal = new PauseSignal();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await signal.WaitIfPausedAsync(cts.Token);
    }
}