namespace SlideGenerator.Domain.Download.Abstractions;

public interface IResolveService
{
    /// <summary>
    ///     Resolves a remote URI and validates it with optional check function.
    /// </summary>
    /// <param name="uri">The URI to resolve.</param>
    /// <param name="handler">The HTTP client handler to use for requests.</param>
    /// <param name="checkUriFunc">The customized check function.</param>
    /// <returns>The resolved URL string.</returns>
    /// <exception cref="InvalidOperationException">The URI is not qualified by check function.</exception>
    Task<Uri> ResolveUriAsync(Uri uri, HttpClientHandler handler,
        Func<Uri, HttpClient, Task<bool>>? checkUriFunc = null);

    Func<Uri, HttpClient, Task<bool>> CheckImageUriFunc { get; }
}