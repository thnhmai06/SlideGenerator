using SlideGenerator.Application.Cloud.Services;
using SlideGenerator.Domain.Cloud.Entities;
using SlideGenerator.Infrastructure.Cloud.Adapters;
using GoogleDriveProvider = SlideGenerator.Infrastructure.Cloud.Adapters.GoogleDriveProvider;
using GooglePhotosProvider = SlideGenerator.Infrastructure.Cloud.Adapters.GooglePhotosProvider;

namespace SlideGenerator.Infrastructure.Cloud.Services;

/// <summary>
///     Resolves URIs using registered cloud providers.
/// </summary>
public sealed class CloudResolver : ICloudResolver
{
    /// <inheritdoc />
    public HashSet<CloudProvider> Providers { get; } =
    [
        GoogleDriveProvider.Instance,
        GooglePhotosProvider.Instance,
        OneDriveProvider.Instance,
        SharePointProvider.Instance
    ];

    /// <inheritdoc />
    public async Task<Uri> ResolveUriAsync(Uri uri, HttpClient? httpClient = null)
    {
        using var client = httpClient ?? new HttpClient();
        using var actuallyUriResponse = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        uri = actuallyUriResponse.RequestMessage?.RequestUri ?? uri;

        foreach (var resolver in Providers.Where(resolver => resolver.IsUriSupported(uri)))
            return await resolver.ResolveUriAsync(uri, client).ConfigureAwait(false);

        return uri;
    }

    /// <inheritdoc />
    public bool IsUriSupported(Uri uri)
    {
        return Providers.Any(resolver => resolver.IsUriSupported(uri));
    }
}