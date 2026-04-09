using System.Web;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Infrastructure.Cloud.Adapters;

/// <summary>
///     Provides access to SharePoint as a cloud provider.
/// </summary>
public sealed class SharePointProvider : CloudProvider
{
    private static readonly Lazy<SharePointProvider> LazyInstance = new(() => new SharePointProvider());

    private SharePointProvider()
    {
    }

    public static SharePointProvider Instance => LazyInstance.Value;

    public override Task<Uri> ResolveUriAsync(Uri supportedUri, HttpClient httpClient)
    {
        var queryParams = HttpUtility.ParseQueryString(supportedUri.Query);
        var fileIdPath = queryParams.Get("id");

        if (!string.IsNullOrEmpty(fileIdPath) && fileIdPath.StartsWith('/'))
        {
            var fullHost = supportedUri.GetLeftPart(UriPartial.Authority);
            supportedUri = new Uri($"{fullHost}{fileIdPath}?download=1");
        }

        return Task.FromResult(supportedUri);
    }

    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase);
    }
}
