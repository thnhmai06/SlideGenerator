using System.Text.RegularExpressions;
using System.Web;

namespace TaoSlideTotNghiep.Utils;

/// <summary>
/// HTTP utility methods for URL processing and file handling.
/// </summary>
public static partial class HttpUtils
{
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();
    private static readonly Regex GoogleDriveFolderFileIdPattern = GoogleDriveFolderFileIdRegex();
    private static readonly Regex GooglePhotosUrlPattern = GooglePhotosUrlRegex();
    private static readonly Dictionary<string, string> MimeToExt = // Only support Image
    new(StringComparer.OrdinalIgnoreCase)
    {
        // JPEG family
        ["image/jpeg"] = "jpg",
        ["image/jpg"] = "jpg",
        ["image/pjpeg"] = "jpg",
        ["image/jp2"] = "jp2",
        ["image/jpx"] = "jpx",
        ["image/jpm"] = "jpm",
        ["image/jpf"] = "jpf",

        // PNG
        ["image/png"] = "png",
        ["image/x-png"] = "png",

        // GIF
        ["image/gif"] = "gif",

        // WebP
        ["image/webp"] = "webp",

        // BMP
        ["image/bmp"] = "bmp",
        ["image/x-bmp"] = "bmp",
        ["image/x-ms-bmp"] = "bmp",

        // TIFF
        ["image/tiff"] = "tiff",
        ["image/tif"] = "tif",
        ["image/x-tiff"] = "tif",

        // HEIF / HEIC
        ["image/heif"] = "heif",
        ["image/heif-sequence"] = "heifs",
        ["image/heic"] = "heic",
        ["image/heic-sequence"] = "heics",
        ["image/heix"] = "heix",
        ["image/heic-x"] = "heic",
        ["image/hei"] = "hei",

        // AVIF
        ["image/avif"] = "avif",
        ["image/avif-sequence"] = "avifs",

        // ICO
        ["image/x-icon"] = "ico",
        ["image/vnd.microsoft.icon"] = "ico",

        // SVG
        ["image/svg+xml"] = "svg",

        // DDS
        ["image/vnd.ms-dds"] = "dds",
        ["image/x-dds"] = "dds",

        // RAW formats (camera)
        ["image/x-adobe-dng"] = "dng",
        ["image/x-canon-cr2"] = "cr2",
        ["image/x-canon-cr3"] = "cr3",
        ["image/x-nikon-nef"] = "nef",
        ["image/x-sony-arw"] = "arw",
        ["image/x-panasonic-raw"] = "rw2",
        ["image/x-olympus-orf"] = "orf",
        ["image/x-fuji-raf"] = "raf",
        ["image/x-pentax-pef"] = "pef",
        ["image/x-sigma-x3f"] = "x3f",
        ["image/x-minolta-mrw"] = "mrw",
        ["image/x-kodak-dcr"] = "dcr",
        ["image/x-kodak-k25"] = "k25",
        ["image/x-kodak-kdc"] = "kdc",

        // PSD / Photoshop
        ["image/vnd.adobe.photoshop"] = "psd",

        // EPS
        ["image/eps"] = "eps",
        ["image/x-eps"] = "eps",
        ["application/postscript"] = "ps",

        // HDR / EXR (High Dynamic Range)
        ["image/vnd.radiance"] = "hdr",
        ["image/x-hdr"] = "hdr",
        ["image/x-exr"] = "exr",
        ["image/exr"] = "exr",

        // JXR / HD Photo / WDP
        ["image/jxr"] = "jxr",
        ["image/vnd.ms-photo"] = "wdp",

        // FITS (scientific imaging)
        ["image/fits"] = "fits",
        ["image/x-fits"] = "fits",

        // PNM family (PBM/PGM/PPM)
        ["image/x-portable-anymap"] = "pnm",
        ["image/x-portable-bitmap"] = "pbm",
        ["image/x-portable-graymap"] = "pgm",
        ["image/x-portable-pixmap"] = "ppm",

        // PCX
        ["image/x-pcx"] = "pcx",

        // XCF (GIMP)
        ["image/x-xcf"] = "xcf",

        // FLIF
        ["image/flif"] = "flif",

        // JPEG XL
        ["image/jxl"] = "jxl",

        // APNG (officially PNG but often separate MIME)
        ["image/apng"] = "apng"
    };


    /// <summary>
    /// Gets file extension from Content-Disposition header, Content-Type, or URL.
    /// </summary>
    public static string? GetFileExtension(HttpResponseMessage response)
    {
        // Content-Disposition
        if (response.Content.Headers.ContentDisposition?.FileName is { } fileName)
        {
            var cleanName = fileName.Trim('"');
            var ext = Path.GetExtension(cleanName).TrimStart('.');
            if (!string.IsNullOrEmpty(ext)) return ext;
        }

        // Content-Type
        if (response.Content.Headers.ContentType?.MediaType is { } mediaType)
        {
            var ext = MimeToExt.GetValueOrDefault(mediaType);
            if (!string.IsNullOrEmpty(ext)) return ext;
        }

        // URL path
        if (response.RequestMessage?.RequestUri?.AbsolutePath is { } path)
        {
            var ext = Path.GetExtension(path).TrimStart('.');
            if (!string.IsNullOrEmpty(ext)) return ext;
        }

        return null;
    }

    /// <summary>
    /// Corrects image URLs from Google Drive, OneDrive, and Google Photos to direct download links.
    /// </summary>
    public static async Task<string> CorrectImageUrl(string imageUrl, HttpClient httpClient)
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
                ? throw new ArgumentException("Cannot extract Google Drive image ID")
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
                : throw new ArgumentException("Cannot extract Google Photos URL");
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
