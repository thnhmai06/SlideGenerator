using System.Text.RegularExpressions;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Infrastructure.Cloud.Adapters;

/// <summary>
///     Provides access to Google Photos as a cloud provider.
/// </summary>
public sealed partial class GooglePhotosProvider : CloudProvider
{
    private static readonly Regex GooglePhotosUrlPattern = GooglePhotosUrlRegex();

    private static readonly Lazy<GooglePhotosProvider> LazyInstance = new(() => new GooglePhotosProvider());

    private GooglePhotosProvider()
    {
    }

    public static GooglePhotosProvider Instance => LazyInstance.Value;

    public override async Task<Uri> ResolveUriAsync(Uri supportedUri, HttpClient httpClient)
    {
        var url = supportedUri.AbsoluteUri;
        var html = await httpClient.GetStringAsync(url).ConfigureAwait(false);
        var match = GooglePhotosUrlPattern.Match(html);
        return match.Success
            ? new Uri(match.Value)
            : supportedUri;
    }

    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith("photos.app.goo.gl", StringComparison.OrdinalIgnoreCase)
               || uri.Host.EndsWith("photos.google.com", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"https://lh3\.googleusercontent\.com/[^""]*", RegexOptions.Compiled)]
    private static partial Regex GooglePhotosUrlRegex();
}