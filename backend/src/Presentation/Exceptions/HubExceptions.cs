namespace Presentation.Exceptions;

/// <summary>
/// Exception thrown when a connection is not found.
/// </summary>
public class ConnectionNotFoundException(string connectionId)
    : InvalidOperationException($"Connection '{connectionId}' not found.")
{
    public string ConnectionId { get; } = connectionId;
}

/// <summary>
/// Exception thrown when a request format is invalid.
/// </summary>
public class InvalidRequestFormatException(string requestType, string? details = null)
    : ArgumentException($"Invalid {requestType} request format{(details != null ? $": {details}" : "")}.")
{
    public string RequestTypeName { get; } = requestType;
    public string? Details { get; } = details;
}
