namespace SlideGenerator.Infrastructure.Common.Utilities;

internal static class UrlUtils
{
    /// <summary>
    ///     Attempts to parse and normalize the specified URL as an absolute HTTP or HTTPS URI.
    /// </summary>
    /// <remarks>
    ///     If the input does not specify a scheme, "https://" is assumed. Only absolute HTTP and HTTPS
    ///     URLs are considered valid.
    /// </remarks>
    /// <param name="rawUrl">The raw URL string to normalize. May be null or empty.</param>
    /// <param name="uri">
    ///     When this method returns, contains the normalized absolute URI if parsing succeeds and the scheme is HTTP or
    ///     HTTPS; otherwise, null.
    /// </param>
    /// <returns>true if the URL was successfully parsed and normalized as an absolute HTTP or HTTPS URI; otherwise, false.</returns>
    public static bool TryNormalizeHttpsUrl(string? rawUrl, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(rawUrl))
            return false;

        rawUrl = rawUrl.Trim();
        if (!rawUrl.Contains("://", StringComparison.Ordinal))
            rawUrl = "https://" + rawUrl;

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var created))
            return false;

        if (created.Scheme != Uri.UriSchemeHttp &&
            created.Scheme != Uri.UriSchemeHttps)
            return false;

        uri = created;
        return true;
    }

    public static bool IsImageFileUrl(string url, HttpClient? httpClient = null)
    {
        httpClient ??= new HttpClient();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = httpClient.Send(request);
            if (response is { IsSuccessStatusCode: true })
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                return contentType != null
                       && contentType.StartsWith("image/",
                           StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Ignore exceptions and treat as non-image URL
        }

        return false;
    }
}