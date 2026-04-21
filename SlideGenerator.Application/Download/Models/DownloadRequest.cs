namespace SlideGenerator.Application.Download.Models;

public record DownloadRequest(string Url, string SaveFolder, string FileName);