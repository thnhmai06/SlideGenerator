namespace SlideGenerator.Presentation.Common.Exceptions.Hubs;

/// <summary>
///     Exception thrown when a request format is invalid.
/// </summary>
public class InvalidRequestFormat(string requestType, string? details = null)
    : ArgumentException($"Invalid {requestType} request format: {(details != null ? $": {details}" : "")}.")
{
    public string RequestTypeName { get; } = requestType;
    public string? Details { get; } = details;
}