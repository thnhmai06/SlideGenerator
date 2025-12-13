namespace TaoSlideTotNghiep.Application.Configs.DTOs.Components;

public record DownloadConfig(int MaxChunks, int LimitBytesPerSecond, string SaveFolder, RetryConfig Retry);