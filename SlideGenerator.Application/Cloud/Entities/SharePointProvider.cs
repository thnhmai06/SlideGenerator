using System.Web;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Application.Cloud.Entities;

/// <summary>
///     Provides access to SharePoint as a cloud provider, enabling resolution and support checks for SharePoint URLs.
/// </summary>
/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 01:32:00 GMT+7
/// </remarks>
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