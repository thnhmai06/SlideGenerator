using System.Text.RegularExpressions;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Provides access to Google Photos as a cloud provider, resolving album URLs to direct image links.
/// </summary>
internal sealed partial class GooglePhotosResolver : CloudResolver
{
    /// <summary>
    ///     A compiled regular expression for extracting direct image URLs from Google Photos HTML content.
    /// </summary>
    private static readonly Regex GooglePhotosUrlPattern = GooglePhotosUrlRegex();

    /// <inheritdoc />
    public override async Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var html = await httpClient.GetStringAsync(supportedUri, cancellationToken).ConfigureAwait(false);
        var match = GooglePhotosUrlPattern.Match(html);
        if (!match.Success) return supportedUri;

        var directUrl = match.Value;
        if (!directUrl.Contains('=') && !directUrl.EndsWith("=d"))
            directUrl += "=d"; // for raw quality

        return new Uri(directUrl);
    }

    /// <inheritdoc />
    public override bool IsUriSupported(Uri uri)
    {
        var host = uri.Host;
        return host.EndsWith("photos.app.goo.gl", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("photos.google.com", StringComparison.OrdinalIgnoreCase) ||
               host.Contains("googleusercontent.com");
    }

    /// <summary>
    ///     Generates the regular expression used to find the direct image URL in Google Photos HTML.
    /// </summary>
    /// <returns>A compiled <see cref="Regex" /> instance.</returns>
    [GeneratedRegex(@"https://lh3\.googleusercontent\.com/pw/[^""\s?]+", RegexOptions.Compiled)]
    private static partial Regex GooglePhotosUrlRegex();
}