namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when saliency computation fails.
/// </summary>
public class ComputeSaliencyFailedException(string filePath)
    : Exception($"Failed to compute saliency map for: {filePath}")
{
    public string FilePath { get; } = filePath;
}

/// <summary>
/// Exception thrown when reading an image fails.
/// </summary>
public class ReadImageFailedException(string filePath) : Exception($"Failed to read image from: {filePath}")
{
    public string FilePath { get; } = filePath;
}
