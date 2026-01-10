namespace SlideGenerator.Application.Features.Jobs;

/// <summary>
///     SignalR group naming helper for job subscriptions.
/// </summary>
public static class JobSignalRGroups
{
    /// <summary>
    ///     Gets the SignalR group name for a group job.
    /// </summary>
    public static string GroupGroup(string groupId)
    {
        return $"group:{groupId}";
    }

    /// <summary>
    ///     Gets the SignalR group name for a sheet job.
    /// </summary>
    public static string SheetGroup(string sheetId)
    {
        return $"sheet:{sheetId}";
    }
}