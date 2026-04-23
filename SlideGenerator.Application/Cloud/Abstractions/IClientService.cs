namespace SlideGenerator.Application.Cloud.Abstractions;

/// <summary>
///     Provides operations to interact with external web servers and APIs.
/// </summary>
public interface IClientService
{
    /// <summary>
    ///     Resolves a URI to its final request URI after handling HTTP redirects.
    /// </summary>
    /// <param name="uri">The <see cref="Uri" /> to request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>The final request <see cref="Uri" /> after redirect handling, or the original URI if unavailable.</returns>
    Task<Uri> GetForwardedUriAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads the HTTP response body as a string.
    /// </summary>
    /// <param name="uri">The <see cref="Uri" /> to request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>The response body as a <see cref="string" />.</returns>
    Task<string> GetBodyAsync(Uri uri, CancellationToken cancellationToken = default);
}