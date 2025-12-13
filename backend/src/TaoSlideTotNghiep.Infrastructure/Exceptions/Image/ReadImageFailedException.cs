namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Image;

/// <summary>
/// Exception thrown when reading an image fails.
/// </summary>
public class ReadImageFailedException(string filePath)
    : Exception($"Failed to read image from: {filePath}")
{
    public string FilePath { get; } = filePath;
}