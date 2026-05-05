using Syncfusion.Presentation;

namespace SlideGenerator.Documents.PowerPoint.Entities;

/// <summary>
///     Wraps a Syncfusion IPresentation and its FileStream for proper disposal and saving.
/// </summary>
public sealed class SfPresentation : IDisposable
{
    private readonly string _filePath;
    private readonly FileStream? _fileStream;
    public IPresentation Value { get; }

    public SfPresentation(string filePath, bool isWritable, string? password = null)
    {
        _filePath = filePath;
        if (isWritable) Value = Presentation.Open(filePath, password);
        else
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Value = Presentation.Open(_fileStream, password);
        }
    }

    public void Save()
    {
        if (_fileStream != null) Value.Save(_fileStream);
        else Value.Save(_filePath);
    }

    public void Dispose()
    {
        Value.Dispose();
        _fileStream?.Dispose();
    }
}
