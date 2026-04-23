using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Settings.Interfaces;

namespace SlideGenerator.Infrastructure.Systems.Adapters;

/// <summary>
///     Implements HTTP operations used by application-level cloud resolvers.
/// </summary>
/// <param name="settingProvider">The setting provider to retrieve proxy configuration.</param>
public sealed class HttpClientService(ISettingProvider settingProvider) : IClientService
{
    /// <summary>
    ///     Gets the final URI after following redirects.
    /// </summary>
    /// <param name="uri">The initial URI.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The final <see cref="Uri" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="uri" /> is null.</exception>
    public async Task<Uri> GetForwardedUriAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var proxySettings = settingProvider.Current.Download.Proxy.GetWebProxy();
        using var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            Proxy = proxySettings
        });
        using var response = await client
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        return response.RequestMessage?.RequestUri ?? uri;
    }

    /// <summary>
    ///     Gets the response body of a URI as a string.
    /// </summary>
    /// <param name="uri">The URI to request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response body as a <see langword="string" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="uri" /> is null.</exception>
    public async Task<string> GetBodyAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        using var client = new HttpClient();
        return await client.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
    }
}