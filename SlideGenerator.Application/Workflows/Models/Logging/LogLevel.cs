namespace SlideGenerator.Application.Workflows.Models.Logging;

/// <summary>
///     Defines the severity levels for workflow execution logs.
/// </summary>
public enum LogLevel
{
    /// <summary>Detailed diagnostic information.</summary>
    Debug,

    /// <summary>General informational messages.</summary>
    Info,

    /// <summary>Indications of potential issues or unexpected conditions.</summary>
    Warn,

    /// <summary>Critical errors that affect execution.</summary>
    Error
}
