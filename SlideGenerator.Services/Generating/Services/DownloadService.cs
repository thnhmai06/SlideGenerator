using Downloader;
using SlideGenerator.Features.Configs.Contracts;
using SlideGenerator.Framework.Features.Cloud.Services;

namespace SlideGenerator.Services.Generating.Services;

/// <summary>
///     Downloads remote files service.
/// </summary>
/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 00:32:20 GMT+7
/// </remarks>
public sealed class DownloadService
{
    private readonly IConfigProvider _configProvider;

    /// <summary>
    ///     Initializes download service with config manager.
    /// </summary>
    /// <param name="configProvider">Read-only configuration manager.</param>
    public DownloadService(IConfigProvider configProvider)
    {
        _configProvider = configProvider;
    }

    /// <summary>
    ///     Downloads a remote image and returns bytes.
    /// </summary>
    /// <param name="uri">image URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="fileName">The customized download file name without extension.</param>
    /// <param name="checkFunc">The customized check function.</param>
    /// <returns>Downloaded bytes.</returns>
    /// <exception cref="InvalidOperationException">The URI is not qualified by check function.</exception>
    public async Task<byte[]> DownloadAsync(
        Uri uri, CancellationToken cancellationToken,
        string? fileName = null,
        Func<Uri, HttpClient, Task<bool>>? checkFunc = null)
    {
        var config = _configProvider.Current;
        var saveFolder = Directory.CreateDirectory(config.Download.SaveFolder);
        var proxy = config.Download.Proxy.GetWebProxy();

        // Resolve URI
        using var resolveClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            Proxy = proxy
        });
        uri = await CloudResolver.Instance.ResolveLinkAsync(uri, resolveClient);
        if (checkFunc != null && !await checkFunc(uri, resolveClient))
            throw new InvalidOperationException($"The URI {uri} is not qualified check function.");
        var url = uri.AbsoluteUri;

        // Download file
        var tempFileFullName = (fileName ?? Guid.NewGuid().ToString()) + ".crdownload";
        var tempFilePath = Path.Combine(saveFolder.FullName, tempFileFullName);
        var configuration = new DownloadConfiguration
        {
            ChunkCount = Math.Max(1, config.Download.MaxChunks),
            MaximumBytesPerSecond = Math.Max(0, config.Download.LimitBytesPerSecond),
            MaxTryAgainOnFailure = Math.Max(0, config.Download.Retry.MaxRetries),
            HttpClientTimeout = Math.Max(1, config.Download.Retry.Timeout * 1000),
            RequestConfiguration = { Proxy = proxy }
        };

        var fileFullName = tempFileFullName + ".bin";
        var filePath = Path.Combine(saveFolder.FullName, fileFullName);
        await using var downloader = new Downloader.DownloadService(configuration);
        downloader.DownloadStarted += (_, e) =>
        {
            var ext = Path.GetExtension(e.FileName);
            fileFullName = string.IsNullOrEmpty(fileName) ? e.FileName : fileName + ext;
        };
        downloader.DownloadFileCompleted += (_, _) => { filePath = Path.Combine(saveFolder.FullName, fileFullName); };
        await downloader.DownloadFileTaskAsync(url, tempFilePath, cancellationToken)
            .ConfigureAwait(false);
        if (File.Exists(tempFilePath))
            File.Move(tempFilePath, filePath);

        // Return and remove
        try
        {
            return await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (config.Download.DeleteAfterDownload && File.Exists(filePath))
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // ignored
                }
        }
    }

    public static async Task<bool> IsImageDownloadUri(Uri uri, HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        return response.IsSuccessStatusCode
               && response.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true;
    }
}