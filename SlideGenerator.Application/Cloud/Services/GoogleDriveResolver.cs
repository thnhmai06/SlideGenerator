using System.Text.RegularExpressions;
using System.Web;
using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Cloud.Rules;
using SlideGenerator.Application.Systems.Abstractions;

namespace SlideGenerator.Application.Cloud.Services;

/// <summary>
///     Provides a cloud provider implementation for accessing and resolving Google Drive file and folder URIs.
/// </summary>
public sealed partial class GoogleDriveResolver(IClientService clientService) : ICloudResolver
{
    /// <summary>
    ///     A compiled regular expression for extracting Google Drive file IDs from URLs.
    /// </summary>
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();

    /// <inheritdoc />
    public async Task<Uri> ResolveUriAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        string? fileId = null;
        var url = uri.AbsoluteUri;

        if (uri.AbsolutePath.Contains("/file/d/"))
        {
            var match = GoogleDriveFileIdPattern.Match(url);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }
        else if (uri.Query.Contains("id="))
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            fileId = query["id"];
        }
        else if (uri.AbsolutePath.Contains("/folders/"))
        {
            var html = await clientService.GetBodyAsync(uri, cancellationToken).ConfigureAwait(false);
            var match = GoogleDriveFileIdPattern.Match(html);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }

        return !string.IsNullOrEmpty(fileId)
            ? new Uri($"https://drive.google.com/uc?export=download&id={fileId}")
            : uri;
    }

    /// <inheritdoc />
    public bool TryIsUriSupported(Uri uri, out CloudResolverKey key)
    {
        if (uri.Host.EndsWith("drive.google.com", StringComparison.OrdinalIgnoreCase))
        {
            key = CloudResolverKey.GoogleDrive;
            return true;
        }

        key = default;
        return false;
    }

    /// <summary>
    ///     Generates the regular expression used to find the file ID in a Google Drive URL.
    /// </summary>
    /// <returns>A compiled <see cref="Regex" /> instance.</returns>
    [GeneratedRegex(@"/file/d/([^/?]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFileIdRegex();
}
