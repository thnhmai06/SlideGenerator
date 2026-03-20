using SlideGenerator.Domain.Download.Abstractions;
using SlideGenerator.Framework.Cloud.Services;

namespace SlideGenerator.Application.Download.Services;

/// Review by @thnhmai06 at 04/03/2026 22:01:09 GMT+7
public class ResolveService : IResolveService
{
    /// <summary>
    ///     Resolves a remote URI and validates it with optional check function.
    /// </summary>
    /// <param name="uri">The URI to resolve.</param>
    /// <param name="handler">The HTTP client handler to use for requests.</param>
    /// <param name="checkUriFunc">The customized check function.</param>
    /// <returns>The resolved URL string.</returns>
    /// <exception cref="InvalidOperationException">The URI is not qualified by check function.</exception>
    public async Task<Uri> ResolveUriAsync(Uri uri, HttpClientHandler handler,
        Func<Uri, HttpClient, Task<bool>>? checkUriFunc = null)
    {
        handler.AllowAutoRedirect = true;
        using var httpClient = new HttpClient(handler);
        uri = await CloudResolver.Instance.ResolveLinkAsync(uri, httpClient);
        if (checkUriFunc != null && !await checkUriFunc(uri, httpClient))
            throw new InvalidOperationException($"The URI {uri} is not qualified check function.");
        return uri;
    }
    public Func<Uri, HttpClient, Task<bool>> CheckImageUriFunc => CheckImageUri;
    
    private static async Task<bool> CheckImageUri(Uri uri, HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        return response.IsSuccessStatusCode
               && response.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true;
    }
}