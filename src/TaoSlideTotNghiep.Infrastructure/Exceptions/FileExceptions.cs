namespace TaoSlideTotNghiep.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a file extension is not supported.
/// </summary>
public class FileExtensionNotSupportedException(string extension)
    : ArgumentException($"File extension '{extension}' is not supported.")
{
    public string Extension { get; } = extension;
}

/// <summary>
/// Exception thrown when unable to extract URL from cloud storage services.
/// </summary>
public class CloudUrlExtractionException(string serviceName, string originalUrl)
    : ArgumentException($"Cannot extract direct download URL from {serviceName}.")
{
    public string ServiceName { get; } = serviceName;
    public string OriginalUrl { get; } = originalUrl;
}