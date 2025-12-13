namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Image;

/// <summary>
/// Exception thrown when saliency computation fails.
/// </summary>
public class ComputeSaliencyFailedException(string filePath)
    : Exception($"Failed to compute saliency map for: {filePath}")
{
    public string FilePath { get; } = filePath;
}