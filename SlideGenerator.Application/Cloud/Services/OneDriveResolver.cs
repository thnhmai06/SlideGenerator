using System.Text;
using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Cloud.Rules;

namespace SlideGenerator.Application.Cloud.Services;

/// <summary>
///     Provides access to Microsoft OneDrive as a cloud provider, converting sharing links to direct API download links.
/// </summary>
public sealed class OneDriveResolver : ICloudResolver
{
    /// <inheritdoc />
    public Task<Uri> ResolveUriAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        var url = uri.AbsoluteUri;
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');
        return Task.FromResult(new Uri($"https://api.onedrive.com/v1.0/shares/{encodedUrl}/root/content"));
    }

    /// <inheritdoc />
    public bool TryIsUriSupported(Uri uri, out CloudResolverKey key)
    {
        var host = uri.Host;
        if (host.EndsWith("1drv.ms", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("onedrive.live.com", StringComparison.OrdinalIgnoreCase))
        {
            key = CloudResolverKey.OneDrive;
            return true;
        }

        key = default;
        return false;
    }
}