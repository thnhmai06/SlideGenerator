using Microsoft.Extensions.Logging;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Defines a contract for resolving cloud-hosted URIs to direct download links.
/// </summary>
internal abstract class CloudResolver(ILogger logger)
{
    protected ILogger Logger { get; } = logger;

    public abstract bool IsUriSupported(Uri uri);

    public abstract Task<Uri> ResolveUriAsync(Uri supportedUri, HttpClient httpClient,
        CancellationToken cancellationToken = default);
}