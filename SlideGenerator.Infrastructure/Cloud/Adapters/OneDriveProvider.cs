using System.Text;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Infrastructure.Cloud.Adapters;

/// <summary>
///     Provides access to OneDrive as a cloud provider.
/// </summary>
public sealed class OneDriveProvider : CloudProvider
{
    private static readonly Lazy<OneDriveProvider> LazyInstance = new(() => new OneDriveProvider());

    private OneDriveProvider()
    {
    }

    public static OneDriveProvider Instance => LazyInstance.Value;

    public override Task<Uri> ResolveUriAsync(Uri supportedUri, HttpClient httpClient)
    {
        var url = supportedUri.AbsoluteUri;
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');
        return Task.FromResult(new Uri($"https://api.onedrive.com/v1.0/shares/{encodedUrl}/root/content"))!;
    }

    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith("1drv.ms", StringComparison.OrdinalIgnoreCase)
               || uri.Host.EndsWith("onedrive.live.com", StringComparison.OrdinalIgnoreCase);
    }
}