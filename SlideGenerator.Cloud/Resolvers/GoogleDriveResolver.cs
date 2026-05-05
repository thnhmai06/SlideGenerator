using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Cloud.Resolvers;

internal sealed partial class GoogleDriveResolver(ILogger logger) : CloudResolver(logger)
{
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();

    public override async Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Resolving Google Drive URI: {Uri}", supportedUri);

        string? fileId = null;
        var url = supportedUri.AbsoluteUri;

        if (supportedUri.AbsolutePath.Contains("/file/d/"))
        {
            var match = GoogleDriveFileIdPattern.Match(url);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }
        else if (supportedUri.Query.Contains("id="))
        {
            var query = HttpUtility.ParseQueryString(supportedUri.Query);
            fileId = query["id"];
        }
        else if (supportedUri.AbsolutePath.Contains("/folders/"))
        {
            Logger.LogDebug("Resolving Google Drive folder URI by fetching HTML content");
            var html = await httpClient.GetStringAsync(supportedUri, cancellationToken).ConfigureAwait(false);
            var match = GoogleDriveFileIdPattern.Match(html);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }

        if (string.IsNullOrEmpty(fileId))
        {
            Logger.LogWarning("Could not extract File ID from Google Drive URI: {Uri}", supportedUri);
            return supportedUri;
        }

        var resolvedUri = new Uri($"https://drive.google.com/uc?export=download&id={fileId}");
        Logger.LogDebug("Resolved Google Drive URI to direct link: {ResolvedUri}", resolvedUri);
        return resolvedUri;
    }

    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith("drive.google.com", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"/file/d/([^/?]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFileIdRegex();
}