using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Systems.Abstractions;

namespace SlideGenerator.Infrastructure.Systems.Adapters;

/// <summary>
///     Implements HTTP operations used by application-level cloud resolvers.
/// </summary>
public sealed class HttpClientService(ISettingProvider settingProvider) : IClientService
{
    /// <inheritdoc />
    public async Task<Uri> GetForwardedUriAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var proxySettings = settingProvider.Current.Download.Proxy.GetWebProxy();
        using var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            Proxy = proxySettings,
        });
        using var response = await client
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        return response.RequestMessage?.RequestUri ?? uri;
    }

    /// <inheritdoc />
    public async Task<string> GetBodyAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        using var client = new HttpClient();
        return await client.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
    }
}

