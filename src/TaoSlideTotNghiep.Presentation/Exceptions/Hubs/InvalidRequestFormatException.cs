namespace TaoSlideTotNghiep.Presentation.Exceptions.Hubs;

/// <summary>
/// Exception thrown when a request format is invalid.
/// </summary>
public class InvalidRequestFormatException(string requestType, string? details = null)
    : ArgumentException($"Invalid {requestType} request format{(details != null ? $": {details}" : "")}.")
{
    public string RequestTypeName { get; } = requestType;
    public string? Details { get; } = details;
}