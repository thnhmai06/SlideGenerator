namespace SlideGenerator.Jobs;

internal enum RowProcessState
{
    Pending,
    InProgress,
    Completed,
    Failed
}