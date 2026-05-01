using SlideGenerator.Application.Modules.Cloud.Abstractions;
using SlideGenerator.Application.Modules.Cloud.Rules;

namespace SlideGenerator.Application.Modules.Cloud.Services;

/// <summary>
///     Manages multiple <see cref="ICloudResolver" /> instances and routes URIs to the appropriate resolver.
/// </summary>
/// <param name="cloudResolvers">The collection of registered cloud resolvers.</param>
/// <param name="clientService">The service used to handle URI redirections.</param>
/// <remarks>
///     This manager iterates through all registered resolvers to find one that supports the provided URI.
///     If no specific resolver matches, it returns the original or redirected URI.
/// </remarks>
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
            if (resolver.TryIsUriSupported(uri, out key))
                return true;

        key = default;
        return false;
    }
}