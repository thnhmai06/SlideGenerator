namespace SlideGenerator.Application.Download.Services;

/// <summary>
///     Provides validation services for URLs and network resources.
/// </summary>
/// <remarks>Review by @thnhmai06 at 04/03/2026 22:01:09 GMT+7</remarks>
public sealed class ValidateService
{
    /// <summary>
    ///     Asynchronously checks if the specified URI points to a valid image resource by reading its HTTP headers.
    /// </summary>
    /// <param name="uri">The <see cref="Uri" /> to validate.</param>
    /// <param name="httpClient">The <see cref="HttpClient" /> used to send the request.</param>
    /// <returns>
    ///     <see langword="true" /> if the URI returns a success status and an "image/*" content type; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static async Task<bool> IsImageUri(Uri uri, HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        return response.IsSuccessStatusCode
               && response.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true;
    }
}