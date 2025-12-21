namespace SlideGenerator.Application.Configs.DTOs.Requests;

/// <summary>
///     Request to update configuration.
/// </summary>
public sealed record ConfigUpdate(
    ServerConfigUpdate? Server,
    DownloadConfigUpdate? Download,
    JobConfigUpdate? Job);

/// <summary>
///     Server configuration update.
/// </summary>
public sealed record ServerConfigUpdate(string Host, int Port, bool Debug);

/// <summary>
///     Download configuration update.
/// </summary>
public sealed record DownloadConfigUpdate(
    int MaxChunks,
    int LimitBytesPerSecond,
    string SaveFolder,
    RetryConfigUpdate Retry);

/// <summary>
///     Download retry configuration update.
/// </summary>
public sealed record RetryConfigUpdate(int Timeout, int MaxRetries);

/// <summary>
///     Job configuration update.
/// </summary>
public sealed record JobConfigUpdate(int MaxConcurrentJobs);
