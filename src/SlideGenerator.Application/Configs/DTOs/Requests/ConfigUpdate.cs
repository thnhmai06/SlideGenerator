namespace SlideGenerator.Application.Configs.DTOs.Requests;

/// <summary>
///     Request to update configuration.
/// </summary>
public sealed record ConfigUpdate(
    ServerConfigUpdate? Server,
    DownloadConfigUpdate? Download,
    JobConfigUpdate? Job,
    ImageConfigUpdate? Image);

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

/// <summary>
///     Image configuration update.
/// </summary>
public sealed record ImageConfigUpdate(FaceConfigUpdate Face, SaliencyConfigUpdate Saliency);

/// <summary>
///     Face configuration update.
/// </summary>
public sealed record FaceConfigUpdate(
    float Confidence,
    float PaddingTop,
    float PaddingBottom,
    float PaddingLeft,
    float PaddingRight,
    bool UnionAll);

/// <summary>
///     Saliency configuration update.
/// </summary>
public sealed record SaliencyConfigUpdate(
    float PaddingTop,
    float PaddingBottom,
    float PaddingLeft,
    float PaddingRight);
