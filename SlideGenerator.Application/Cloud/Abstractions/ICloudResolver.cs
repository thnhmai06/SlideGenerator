using SlideGenerator.Application.Cloud.Rules;

namespace SlideGenerator.Application.Cloud.Abstractions;

/// <summary>
///     Defines a cloud URI resolver contract.
/// </summary>
public interface ICloudResolver
{
    /// <summary>
    ///     Tries to determine the resolver key that can process the specified URI.
    /// </summary>
    /// <param name="uri">The URI to evaluate.</param>
    /// <param name="key">When this method returns <see langword="true" />, contains the matched resolver key.</param>
    /// <returns><see langword="true" /> when the URI is supported; otherwise <see langword="false" />.</returns>
    bool TryIsUriSupported(Uri uri, out CloudResolverKey key);

    /// <summary>
    ///     Resolves a URI to a direct resource URI.
    /// </summary>
    /// <param name="uri">The URI to resolve.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>The resolved URI.</returns>
    Task<Uri> ResolveUriAsync(Uri uri, CancellationToken cancellationToken = default);
}

