using System.Text.RegularExpressions;
using System.Web;

namespace SlideGenerator.Cloud.Resolvers;

internal sealed partial class GoogleDriveResolver : CloudResolver
{
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();

    public override async Task<Uri> ResolveUriAsync(
        Uri supportedUri, 
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
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
            var html = await httpClient.GetStringAsync(supportedUri, cancellationToken).ConfigureAwait(false);
            var match = GoogleDriveFileIdPattern.Match(html);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }

        return !string.IsNullOrEmpty(fileId)
            ? new Uri($"https://drive.google.com/uc?export=download&id={fileId}")
            : supportedUri;
    }

    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith("drive.google.com", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"/file/d/([^/?]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFileIdRegex();
}
