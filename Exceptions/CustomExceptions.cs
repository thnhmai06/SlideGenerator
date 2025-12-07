namespace TaoSlideTotNghiep.Exceptions;

/// <summary>
/// Exception thrown when a file extension is not supported.
/// </summary>
public class FileExtensionNotSupportedException(string extension)
    : ArgumentException($"File extension '{extension}' is not supported.")
{
    public string Extension { get; } = extension;
}

/// <summary>
/// Exception thrown when an index is out of the valid range.
/// </summary>
public class IndexOutOfRangeException(int index, (int min, int max)? range = null) : ArgumentOutOfRangeException(
    $"Index {index} is out of range{(range.HasValue ? $" ({range.Value.min}, {range.Value.max})" : "")}.")
{
    public int Index { get; } = index;
    public (int Min, int Max)? Range { get; } = range;
}

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

/// <summary>
/// Exception thrown when request does not include a valid type.
/// </summary>
public class TypeNotIncludedException(Type enumType) : ArgumentException(
    $"Type is not included in request/action. Must be one of [{string.Join(", ", Enum.GetNames(enumType))}].")
{
    public Type EnumType { get; } = enumType;
}
