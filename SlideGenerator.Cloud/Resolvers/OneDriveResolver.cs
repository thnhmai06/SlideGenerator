using System.Text;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Provides access to Microsoft OneDrive as a cloud provider, converting sharing links to direct API download links.
/// </summary>
internal sealed class OneDriveResolver : CloudResolver
{
    /// <inheritdoc />
    public override Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var url = supportedUri.AbsoluteUri;
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');
        return Task.FromResult(new Uri($"https://api.onedrive.com/v1.0/shares/{encodedUrl}/root/content"));
    }

    /// <inheritdoc />
    public override bool IsUriSupported(Uri uri)
    {
        var host = uri.Host;
        return host.EndsWith("1drv.ms", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith("onedrive.live.com", StringComparison.OrdinalIgnoreCase);
    }
}