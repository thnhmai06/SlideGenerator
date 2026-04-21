using System.Web;
using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Cloud.Rules;

namespace SlideGenerator.Application.Cloud.Services;

/// <summary>
///     Provides access to SharePoint as a cloud provider.
/// </summary>
public sealed class SharePointResolver : ICloudResolver
{
    /// <inheritdoc />
    public Task<Uri> ResolveUriAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(uri.Query)) 
            return Task.FromResult(uri);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        var fileIdPath = queryParams.Get("id");

        if (!string.IsNullOrEmpty(fileIdPath) && fileIdPath.StartsWith('/'))
        {
            var fullHost = uri.GetLeftPart(UriPartial.Authority);
            uri = new Uri($"{fullHost}{fileIdPath}?download=1");
        }

        return Task.FromResult(uri);
    }

    /// <inheritdoc />
    public bool TryIsUriSupported(Uri uri, out CloudResolverKey key)
    {
        if (uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase))
        {
            key = CloudResolverKey.SharePoint;
            return true;
        }

        key = default;
        return false;
    }
}