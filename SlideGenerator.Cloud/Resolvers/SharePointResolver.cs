using System.Web;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Provides access to Microsoft SharePoint as a cloud provider, resolving file URIs to direct download links.
/// </summary>
internal sealed class SharePointResolver : CloudResolver
{
    /// <inheritdoc />
    public override Task<Uri> ResolveUriAsync(Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(supportedUri.Query))
            return Task.FromResult(supportedUri);
        var queryParams = HttpUtility.ParseQueryString(supportedUri.Query);
        var fileIdPath = queryParams.Get("id");

        if (!string.IsNullOrEmpty(fileIdPath) && fileIdPath.StartsWith('/'))
        {
            var fullHost = supportedUri.GetLeftPart(UriPartial.Authority);
            supportedUri = new Uri($"{fullHost}{fileIdPath}?download=1");
        }

        return Task.FromResult(supportedUri);
    }

    /// <inheritdoc />
    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase);
    }
}
