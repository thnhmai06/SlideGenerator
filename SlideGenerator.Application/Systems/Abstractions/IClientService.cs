namespace SlideGenerator.Application.Systems.Abstractions;

/// <summary>
///     Provides operations to get information from the server.
/// </summary>
public interface IClientService
{
    /// <summary>
    ///     Resolves a URI to its final request URI after redirect handling.
    /// </summary>
    /// <param name="uri">The URI to request.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>The final request URI after redirect handling, or the original URI if unavailable.</returns>
    Task<Uri> GetForwardedUriAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads the response body as a string.
    /// </summary>
    /// <param name="uri">The URI to request.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>The response body as a string.</returns>
    Task<string> GetBodyAsync(Uri uri, CancellationToken cancellationToken = default);
}

