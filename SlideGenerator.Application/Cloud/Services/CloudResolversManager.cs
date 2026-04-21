using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Cloud.Rules;
using SlideGenerator.Application.Systems.Abstractions;

namespace SlideGenerator.Application.Cloud.Services;

/// <summary>
///     Resolves cloud-hosted URIs by routing to keyed cloud URI resolvers.
/// </summary>
public sealed class CloudResolversManager(IEnumerable<ICloudResolver> cloudResolvers, IClientService clientService)
    : ICloudResolver
{
    /// <inheritdoc />
    public async Task<Uri> ResolveUriAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var normalizedUri = await clientService
            .GetForwardedUriAsync(uri, cancellationToken)
            .ConfigureAwait(false);

        foreach (var resolver in cloudResolvers)
        {
            if (!resolver.TryIsUriSupported(normalizedUri, out _))
                continue;

            return await resolver
                .ResolveUriAsync(normalizedUri, cancellationToken)
                .ConfigureAwait(false);
        }

        return normalizedUri;
    }

    /// <inheritdoc />
    public bool TryIsUriSupported(Uri uri, out CloudResolverKey key)
    {
        foreach (var resolver in cloudResolvers)
        {
            if (resolver.TryIsUriSupported(uri, out key))
                return true;
        }

        key = default;
        return false;
    }
}