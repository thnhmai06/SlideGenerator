using System.Text.RegularExpressions;
using System.Web;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Infrastructure.Cloud.Adapters;

/// <summary>
///     Provides a cloud provider implementation for accessing and resolving Google Drive file and folder URIs.
/// </summary>
public sealed partial class GoogleDriveProvider : CloudProvider
{
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();
    private static readonly Regex GoogleDriveFolderFileIdPattern = GoogleDriveFolderFileIdRegex();

    private static readonly Lazy<GoogleDriveProvider> LazyInstance = new(() => new GoogleDriveProvider());

    private GoogleDriveProvider()
    {
    }

    public static GoogleDriveProvider Instance => LazyInstance.Value;

    public override async Task<Uri> ResolveUriAsync(Uri supportedUri, HttpClient httpClient)
    {
        string? fileId = null;
        var url = supportedUri.AbsoluteUri;

        if (supportedUri.AbsolutePath.Contains("/file/d/"))
        {
            var match = GoogleDriveFileIdPattern.Match(url);
            if (match.Success) fileId = match.Groups[1].Value;
        }
        else if (supportedUri.Query.Contains("id="))
        {
            var query = HttpUtility.ParseQueryString(supportedUri.Query);
            fileId = query["id"];
        }
        else if (supportedUri.AbsolutePath.Contains("/folders/"))
        {
            var html = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            var match = GoogleDriveFolderFileIdPattern.Match(html);
            if (match.Success) fileId = match.Groups[1].Value;
        }

        return !string.IsNullOrEmpty(fileId)
            ? new Uri($"https://drive.google.com/uc?export=download&id={fileId}")
            : supportedUri;
    }

    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith("drive.google.com", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"/file/d/([^/]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFileIdRegex();

    [GeneratedRegex(@"/file/d/([^\\]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFolderFileIdRegex();
}