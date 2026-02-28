namespace SlideGenerator.Features.Jobs;

internal sealed class JobControlState
{
    public volatile bool IsCancelled;
    public volatile bool IsPaused;
}