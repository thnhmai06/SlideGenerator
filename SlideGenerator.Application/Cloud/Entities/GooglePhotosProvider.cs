using System.Text.RegularExpressions;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Application.Cloud.Entities;

/// <summary>
///     Provides access to Google Photos as a cloud provider, enabling resolution and support checks for Google Photos
///     URLs.
/// </summary>
/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 01:02:53 GMT+7
///     This class implements the singleton pattern. Use the <see cref="Instance" /> property to access the
///     shared instance. GooglePhotosProvider supports URLs from both photos.app.goo.gl and photos.google.com
///     domains.
/// </remarks>
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