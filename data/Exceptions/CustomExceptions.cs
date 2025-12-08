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

/// <summary>
/// Exception thrown when a connection is not found.
/// </summary>
public class ConnectionNotFoundException(string connectionId)
    : InvalidOperationException($"Connection '{connectionId}' not found.")
{
    public string ConnectionId { get; } = connectionId;
}

/// <summary>
/// Exception thrown when a table/sheet is not found in a workbook.
/// </summary>
public class TableNotFoundException(string tableName, string? workbookPath = null)
    : KeyNotFoundException($"Table '{tableName}' not found{(workbookPath != null ? $" in workbook '{workbookPath}'" : "")}.")
{
    public string TableName { get; } = tableName;
    public string? WorkbookPath { get; } = workbookPath;
}

/// <summary>
/// Exception thrown when a request format is invalid.
/// </summary>
public class InvalidRequestFormatException(string requestType, string? details = null)
    : ArgumentException($"Invalid {requestType} request format{(details != null ? $": {details}" : "")}.")
{
    public string RequestTypeName { get; } = requestType;
    public string? Details { get; } = details;
}

/// <summary>
/// Exception thrown when unable to extract URL from cloud storage services.
/// </summary>
public class CloudUrlExtractionException(string serviceName, string originalUrl)
    : ArgumentException($"Cannot extract direct download URL from {serviceName}.")
{
    public string ServiceName { get; } = serviceName;
    public string OriginalUrl { get; } = originalUrl;
}
