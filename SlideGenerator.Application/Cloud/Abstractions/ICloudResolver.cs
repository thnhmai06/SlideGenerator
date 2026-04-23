using SlideGenerator.Application.Cloud.Rules;

namespace SlideGenerator.Application.Cloud.Abstractions;

/// <summary>
///     Defines a contract for resolving cloud-hosted URIs to direct download links.
/// </summary>
public interface ICloudResolver
{
    /// <summary>
    ///     Evaluates whether the specified URI is supported by this resolver.
    /// </summary>
    /// <param name="uri">The <see cref="Uri" /> to evaluate.</param>
    /// <param name="key">When this method returns <see langword="true" />, contains the matched <see cref="CloudResolverKey" />.</param>
    /// <returns><see langword="true" /> if the URI is supported; otherwise, <see langword="false" />.</returns>
    bool TryIsUriSupported(Uri uri, out CloudResolverKey key);

    /// <summary>
    ///     Resolves a cloud-hosted URI to a direct, downloadable resource URI.
    /// </summary>
    /// <param name="uri">The <see cref="Uri" /> to resolve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>The resolved direct <see cref="Uri" />.</returns>
    Task<Uri> ResolveUriAsync(Uri uri, CancellationToken cancellationToken = default);
}
