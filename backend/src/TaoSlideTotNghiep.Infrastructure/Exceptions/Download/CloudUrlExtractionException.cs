namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Download;

/// <summary>
/// Exception thrown when unable to extract URL from cloud storage services.
/// </summary>
public class CloudUrlExtractionException(string serviceName, string originalUrl)
    : ArgumentException($"Cannot extract direct download URL from {serviceName}.")
{
    public string ServiceName { get; } = serviceName;
    public string OriginalUrl { get; } = originalUrl;
}