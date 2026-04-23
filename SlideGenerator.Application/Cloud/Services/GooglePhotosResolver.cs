using System.Text.RegularExpressions;
using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Cloud.Rules;

namespace SlideGenerator.Application.Cloud.Services;

/// <summary>
///     Provides access to Google Photos as a cloud provider, resolving album URLs to direct image links.
/// </summary>
public sealed partial class GooglePhotosResolver(IClientService clientService) : ICloudResolver
{
    /// <summary>
    ///     A compiled regular expression for extracting direct image URLs from Google Photos HTML content.
    /// </summary>
    private static readonly Regex GooglePhotosUrlPattern = GooglePhotosUrlRegex();

    /// <inheritdoc />
    public async Task<Uri> ResolveUriAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        var html = await clientService.GetBodyAsync(uri, cancellationToken).ConfigureAwait(false);
        var match = GooglePhotosUrlPattern.Match(html);
        if (!match.Success) return uri;

        var directUrl = match.Value;
        if (!directUrl.Contains('=') && !directUrl.EndsWith("=d"))
            directUrl += "=d"; // for raw quality

        return new Uri(directUrl);
    }

    /// <inheritdoc />
    public bool TryIsUriSupported(Uri uri, out CloudResolverKey key)
    {
        var host = uri.Host;
        if (host.EndsWith("photos.app.goo.gl", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("photos.google.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("googleusercontent.com"))
        {
            key = CloudResolverKey.GooglePhotos;
            return true;
        }

        key = default;
        return false;
    }

    /// <summary>
    ///     Generates the regular expression used to find the direct image URL in Google Photos HTML.
    /// </summary>
    /// <returns>A compiled <see cref="Regex" /> instance.</returns>
    [GeneratedRegex(@"https://lh3\.googleusercontent\.com/pw/[^""\s?]+", RegexOptions.Compiled)]
    private static partial Regex GooglePhotosUrlRegex();
}