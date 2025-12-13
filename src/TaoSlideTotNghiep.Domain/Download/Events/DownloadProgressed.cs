namespace TaoSlideTotNghiep.Domain.Download.Events;

public record DownloadProgressed(long BytesReceived, long TotalBytes, double ProgressPercentage);