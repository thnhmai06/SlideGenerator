using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Domain.Cloud.Abstractions;

public interface ICloudResolver
{
    /// <summary>
    ///     Gets the set of cloud providers associated with this instance.
    /// </summary>
    /// <remarks>
    ///     The set may be empty if no providers have been added. Modifying this collection directly
    ///     affects the providers associated with the instance.
    /// </remarks>
    HashSet<CloudProvider> Providers { get; }

    /// <summary>
    ///     Resolves the given URI using the appropriate cloud provider.
    /// </summary>
    /// <param name="uri">The URI to resolve.</param>
    /// <param name="httpClient">An optional HttpClient to use for network requests.</param>
    /// <returns>The resolved URI, or actually URI when use GET if resolution fails.</returns>
    Task<Uri> ResolveUriAsync(Uri uri, HttpClient? httpClient = null);

    /// <summary>
    ///     Checks if the URI is from a supported cloud service.
    /// </summary>
    /// <param name="uri">The URI to check.</param>
    /// <returns><see langword="true" /> if the URI is from a supported cloud service, otherwise <see langword="false" />.</returns>
    bool IsUriSupported(Uri uri);
}