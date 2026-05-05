using System.Text;
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Provides access to Microsoft OneDrive as a cloud provider, converting sharing links to direct API download links.
/// </summary>
internal sealed class OneDriveResolver(ILogger logger) : CloudResolver(logger)
{
    /// <inheritdoc />
    public override Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Resolving OneDrive URI: {Uri}", supportedUri);

        var url = supportedUri.AbsoluteUri;
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');

        var resolvedUri = new Uri($"https://api.onedrive.com/v1.0/shares/{encodedUrl}/root/content");
        Logger.LogDebug("Resolved OneDrive URI to direct link: {ResolvedUri}", resolvedUri);

        return Task.FromResult(resolvedUri);
    }

    /// <inheritdoc />
    public override bool IsUriSupported(Uri uri)
    {
        var host = uri.Host;
        return host.EndsWith("1drv.ms", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith("onedrive.live.com", StringComparison.OrdinalIgnoreCase);
    }
}