using System.Net.Http.Headers;
using Downloader;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Configs;
using SlideGenerator.Framework.Cloud;
using SlideGenerator.Infrastructure.Image.Exceptions;
using SlideGenerator.Infrastructure.Utilities;

namespace SlideGenerator.Infrastructure.Download.Models;

/// <summary>
///     Represents a generic download task wrapping Downloader.DownloadService.
/// </summary>
public sealed class DownloadImageTask(string url, DirectoryInfo saveFolder, ILoggerFactory? loggerFactory = null)
    : DownloadTask(url, saveFolder, new RequestConfiguration
    {
        Accept = "image/*",
        Proxy = ConfigHolder.Value.Download.Proxy.GetWebProxy()
    }, loggerFactory)
{
    public override async Task DownloadFileAsync()
    {
        var httpClient = new HttpClient(new HttpClientHandler
        {
            UseProxy = true,
            Proxy = ConfigHolder.Value.Download.Proxy.GetWebProxy(),
            AllowAutoRedirect = true
        });

        var resolvedUri = await CloudUrlResolver.ResolveLinkAsync(Url, httpClient);
        Url = resolvedUri.ToString();

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        if (!UrlUtils.IsImageFileUrl(Url, httpClient))
            throw new NotImageFileUrl(Url);

        await base.DownloadFileAsync();
    }
}