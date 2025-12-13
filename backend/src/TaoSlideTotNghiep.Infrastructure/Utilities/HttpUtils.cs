using System.Text.RegularExpressions;
using System.Web;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Download;

namespace TaoSlideTotNghiep.Infrastructure.Utilities;

/// <summary>
/// HTTP utility methods for URL processing and file handling.
/// </summary>
public static partial class HttpUtils
{
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();
    private static readonly Regex GoogleDriveFolderFileIdPattern = GoogleDriveFolderFileIdRegex();
    private static readonly Regex GooglePhotosUrlPattern = GooglePhotosUrlRegex();

    /// <summary>
    /// Corrects image URLs from Google Drive, OneDrive, and Google Photos to direct download links.
    /// </summary>
    public static async Task<string> CorrectImageUrlAsync(string imageUrl, HttpClient httpClient)
    {
        // Google Drive link
        if (imageUrl.Contains("drive.google.com"))
        {
            string? imageId = null;

            if (imageUrl.Contains("/file/d/"))
            {
                var match = GoogleDriveFileIdPattern.Match(imageUrl);
                if (match.Success) imageId = match.Groups[1].Value;
            }
            else if (imageUrl.Contains("id="))
            {
                var uri = new Uri(imageUrl);
                var query = HttpUtility.ParseQueryString(uri.Query);
                imageId = query["id"];
            }
            else if (imageUrl.Contains("/folders/"))
            {
                var html = await httpClient.GetStringAsync(imageUrl);
                var match = GoogleDriveFolderFileIdPattern.Match(html);
                if (match.Success) imageId = match.Groups[1].Value;
            }

            return string.IsNullOrEmpty(imageId)
                ? throw new CloudUrlExtractionException("Google Drive", imageUrl)
                : $"https://drive.google.com/uc?export=download&id={imageId}";
        }

        // OneDrive link
        if (imageUrl.Contains("1drv.ms") || imageUrl.Contains("onedrive.live.com"))
        {
            var shareToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(imageUrl))
                .TrimEnd('=');
            return $"https://api.onedrive.com/v1.0/shares/u!{shareToken}/root/content";
        }

        // Google Photos link
        if (imageUrl.Contains("photos.app.goo.gl") || imageUrl.Contains("photos.google.com"))
        {
            var html = await httpClient.GetStringAsync(imageUrl);
            var match = GooglePhotosUrlPattern.Match(html);
            return match.Success
                ? match.Value
                : throw new CloudUrlExtractionException("Google Photos", imageUrl);
        }

        // Direct link
        return imageUrl;
    }

    [GeneratedRegex(@"/file/d/([^/]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFileIdRegex();

    [GeneratedRegex(@"/file/d/([^\\]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFolderFileIdRegex();

    [GeneratedRegex(@"https://lh3\.googleusercontent\.com/[^""]*", RegexOptions.Compiled)]
    private static partial Regex GooglePhotosUrlRegex();
}