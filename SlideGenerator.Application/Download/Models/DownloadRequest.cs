namespace SlideGenerator.Application.Download.Models;

/// <summary>
///     Represents the necessary data to request a file download.
/// </summary>
/// <param name="Url">The direct URL of the file to download.</param>
/// <param name="SaveFolder">The absolute directory path where the file will be saved.</param>
/// <param name="FileName">The desired name of the downloaded file (without extension).</param>
public record DownloadRequest(string Url, string SaveFolder, string FileName);
