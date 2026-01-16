using Hangfire;
using Hangfire.Common;
using Hangfire.Dashboard;

namespace SlideGenerator.Infrastructure.Features.Jobs.Hangfire;

/// <summary>
///     Custom attribute to display sheet job names as "GroupName/SheetName" in Hangfire dashboard.
/// </summary>
public sealed class SheetJobDisplayNameAttribute : JobDisplayNameAttribute
{
    /// <summary>
    ///     Creates a new instance of the attribute.
    /// </summary>
    public SheetJobDisplayNameAttribute() : base("{0}")
    {
    }

    /// <inheritdoc />
    public override string Format(DashboardContext context, Job job)
    {
        // Try to get the sheet ID from the job arguments
        if (job.Args is not { Count: > 0 })
            return "Unknown Job";

        var sheetId = job.Args[0]?.ToString();
        if (string.IsNullOrEmpty(sheetId))
            return "Unknown Job";

        // Try to resolve the display name from the job name registry
        var displayName = SheetJobNameRegistry.GetDisplayName(sheetId);
        return displayName ?? $"Sheet: {sheetId[..Math.Min(8, sheetId.Length)]}...";
    }
}