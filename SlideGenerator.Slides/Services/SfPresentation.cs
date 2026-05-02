using Syncfusion.Presentation;

namespace SlideGenerator.Slides.Services;

/// <summary>
///     Wraps a Syncfusion IPresentation and its FileStream for proper disposal and saving.
/// </summary>
public sealed class SfPresentation : IDisposable
{
    private readonly FileStream? _fileStream;
    public IPresentation Value { get; }

    public SfPresentation(string filePath, bool isWritable, string? password)
    {
        if (isWritable) Value = Presentation.Open(filePath, password);
        else
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Value = Presentation.Open(_fileStream, password);
        }
    }

    public void Dispose()
    {
        Value.Dispose();
        
        _fileStream?.Dispose();
    }
}