using SlideGenerator.Application.Cloud.Entities;
using SlideGenerator.Domain.Cloud.Abstractions;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Application.Cloud.Services;

/// <summary>
///     Provides a singleton service for resolving URIs using a set of supported cloud providers.
/// </summary>
/// <remarks>
///     <see cref="CloudResolver" /> maintains a collection of cloud providers and delegates URI resolution to the
///     appropriate provider based on the input URI. The set of providers can be accessed and modified through the
///     Providers
///     property. This class is thread-safe for read operations, but modifications to the Providers collection should be
///     synchronized if accessed concurrently.
/// </remarks>
/// Reviewed by @thnhmai06 at 01/03/2026 01:33:43 GMT+7
public sealed class CloudResolver : ICloudResolver
{
    /// <summary>
    ///     Gets the set of cloud providers associated with this instance.
    /// </summary>
    /// <remarks>
    ///     The set may be empty if no providers have been added. Modifying this collection directly
    ///     affects the providers associated with the instance.
    /// </remarks>
    public HashSet<CloudProvider> Providers { get; } =
    [
        GoogleDriveProvider.Instance,
        GooglePhotosProvider.Instance,
        OneDriveProvider.Instance,
        SharePointProvider.Instance
    ];

    /// <summary>
    ///     Resolves the given URI using the appropriate cloud provider.
    /// </summary>
    /// <param name="uri">The URI to resolve.</param>
    /// <param name="httpClient">An optional HttpClient to use for network requests.</param>
    /// <returns>The resolved URI, or actually URI when use GET if resolution fails.</returns>
    public async Task<Uri> ResolveUriAsync(Uri uri, HttpClient? httpClient = null)
    {
        // Get actually Uri
        using var client = httpClient ?? new HttpClient();
        using var actuallyUriResponse = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        uri = actuallyUriResponse.RequestMessage?.RequestUri ?? uri;

        // Resolve uri
        foreach (var resolver in Providers.Where(resolver => resolver.IsUriSupported(uri)))
            return await resolver.ResolveUriAsync(uri, client).ConfigureAwait(false);
        return uri;
    }

    /// <summary>
    ///     Checks if the URI is from a supported cloud service.
    /// </summary>
    /// <param name="uri">The URI to check.</param>
    /// <returns><see langword="true" /> if the URI is from a supported cloud service, otherwise <see langword="false" />.</returns>
    public bool IsUriSupported(Uri uri)
    {
        return Providers.Any(resolver => resolver.IsUriSupported(uri));
    }
}