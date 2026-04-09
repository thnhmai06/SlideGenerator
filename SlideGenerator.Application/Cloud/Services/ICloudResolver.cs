using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Application.Cloud.Services;

/// <summary>
///     Resolves cloud-hosted URIs using registered providers.
/// </summary>
public interface ICloudResolver
{
    /// <summary>
    ///     Gets the set of cloud providers associated with this instance.
    /// </summary>
    HashSet<CloudProvider> Providers { get; }

    /// <summary>
    ///     Resolves the given URI using the appropriate cloud provider.
    /// </summary>
    /// <param name="uri">The URI to resolve.</param>
    /// <param name="httpClient">An optional HTTP client to use for network requests.</param>
    /// <returns>The resolved URI, or the original URI when no specialized resolution is required.</returns>
    Task<Uri> ResolveUriAsync(Uri uri, HttpClient? httpClient = null);

    /// <summary>
    ///     Checks if the URI is supported by any registered provider.
    /// </summary>
    /// <param name="uri">The URI to test.</param>
    /// <returns><see langword="true" /> when at least one provider supports the URI; otherwise <see langword="false" />.</returns>
    bool IsUriSupported(Uri uri);
}