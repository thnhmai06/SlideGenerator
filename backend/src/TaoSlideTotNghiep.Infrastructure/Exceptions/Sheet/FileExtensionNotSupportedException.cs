namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Sheet;

/// <summary>
/// Exception thrown when a file extension is not supported.
/// </summary>
public class FileExtensionNotSupportedException(string extension)
    : ArgumentException($"File extension '{extension}' is not supported.")
{
    public string Extension { get; } = extension;
}