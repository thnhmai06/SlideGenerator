namespace SlideGenerator.Presentation.Exceptions.Hubs;

/// <summary>
///     Exception thrown when a connection is not found.
/// </summary>
public class ConnectionNotFoundException(string connectionId)
    : InvalidOperationException($"Connection '{connectionId}' not found.")
{
    public string ConnectionId { get; } = connectionId;
}