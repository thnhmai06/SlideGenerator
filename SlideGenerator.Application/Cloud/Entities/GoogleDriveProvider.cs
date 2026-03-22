using System.Text.RegularExpressions;
using System.Web;
using SlideGenerator.Domain.Cloud.Entities;

namespace SlideGenerator.Application.Cloud.Entities;

/// <summary>
///     Provides a cloud provider implementation for accessing and resolving Google Drive file and folder URIs.
/// </summary>
/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 01:02:27 GMT+7
///     Use this class to interact with Google Drive resources in a standardized way within the application.
///     The provider supports resolving direct download links for files and the first file in a folder, given a supported
///     Google Drive URI. This class is implemented as a singleton; use the <see cref="Instance" /> property to access the
///     shared instance.
/// </remarks>
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

        // File
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
        // Folder
        else if (supportedUri.AbsolutePath.Contains("/folders/"))
        {
            var html = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            var match = GoogleDriveFolderFileIdPattern.Match(html);
            if (match.Success) fileId = match.Groups[1].Value; // first file of folder
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