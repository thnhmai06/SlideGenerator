using SlideGenerator.Cloud.Models;
using SlideGenerator.Cloud.Resolvers;

namespace SlideGenerator.Cloud.Services;

public sealed class MultiCloudResolver
{
    private readonly IReadOnlyDictionary<CloudResolverKey, CloudResolver> _resolvers =
        new Dictionary<CloudResolverKey, CloudResolver>
        {
            { CloudResolverKey.GoogleDrive, new GoogleDriveResolver() },
            { CloudResolverKey.GooglePhotos, new GooglePhotosResolver() },
            { CloudResolverKey.OneDrive, new OneDriveResolver() },
            { CloudResolverKey.SharePoint, new SharePointResolver() }
        }.AsReadOnly();

    public bool IsUriSupported(Uri uri, out CloudResolverKey key)
    {
        foreach (var kvp in _resolvers)
        {
            if (kvp.Value.IsUriSupported(uri))
            {
                key = kvp.Key;
                return true;
            }
        }

        key = default;
        return false;
    }

    public async Task<Uri> ResolveUriAsync(
        Uri uri, HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (IsUriSupported(uri, out var key))
            return await _resolvers[key].ResolveUriAsync(uri, httpClient, cancellationToken)
                .ConfigureAwait(false);
        return uri;
    }
}