using System.Collections.Concurrent;

namespace SlideGenerator.Infrastructure.Features.Jobs.Hangfire;

/// <summary>
///     Thread-safe registry that stores display names for sheet jobs.
///     Format: "WorkbookName/SheetName"
/// </summary>
public static class SheetJobNameRegistry
{
    private static readonly ConcurrentDictionary<string, string> DisplayNames = new();

    /// <summary>
    ///     Registers a display name for a sheet job.
    /// </summary>
    /// <param name="sheetId">The sheet job ID</param>
    /// <param name="workbookName">The workbook/group name</param>
    /// <param name="sheetName">The sheet name</param>
    public static void Register(string sheetId, string workbookName, string sheetName)
    {
        var displayName = $"{workbookName}/{sheetName}";
        DisplayNames[sheetId] = displayName;
    }

    /// <summary>
    ///     Gets the display name for a sheet job.
    /// </summary>
    /// <param name="sheetId">The sheet job ID</param>
    /// <returns>The display name, or null if not found</returns>
    public static string? GetDisplayName(string sheetId)
    {
        return DisplayNames.GetValueOrDefault(sheetId);
    }

    /// <summary>
    ///     Removes a sheet job from the registry.
    /// </summary>
    /// <param name="sheetId">The sheet job ID</param>
    public static void Unregister(string sheetId)
    {
        DisplayNames.TryRemove(sheetId, out _);
    }

    /// <summary>
    ///     Clears all registered display names.
    /// </summary>
    public static void Clear()
    {
        DisplayNames.Clear();
    }
}