using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Rules;
using SfPresNs = Syncfusion.Presentation;

// NOTE: Syncfusion.Presentation does not expose a PresentationSaveFormat enum on IPresentation.Save().
// Format is determined by the file extension of the stream or the file path passed to Save().
// For in-place saves (same stream), use Save(stream) which preserves the original format.

namespace SlideGenerator.Infrastructure.Slides.Adapters;

/// <summary>
///     Represents a PowerPoint presentation backed by the Syncfusion Presentation library.
/// </summary>
public sealed class SfPresentation : IPresentation, IDisposable
{
    private FileStream? _fileStream;

    private readonly Lazy<SfPresNs.IPresentation> _core;

    public SfPresentation(string filePath, bool isEditable = true)
    {
        Identifier = new PresentationIdentifier(filePath);

        _core = new Lazy<SfPresNs.IPresentation>(() =>
        {
            var access = isEditable ? FileAccess.ReadWrite : FileAccess.Read;
            var share = isEditable ? FileShare.Read : FileShare.ReadWrite;
            _fileStream = new FileStream(filePath, FileMode.Open, access, share);
            return SfPresNs.Presentation.Open(_fileStream);
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public PresentationIdentifier Identifier { get; }

    /// <inheritdoc />
    public IEnumerable<ISlide> EnumerateSlides()
    {
        var slides = _core.Value.Slides;
        for (var i = 0; i < slides.Count; i++)
            yield return new SfSlide(this, slides[i], i + 1);
    }

    /// <inheritdoc />
    public ISlide CopySlide(int from, int to)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(from);

        var slides = _core.Value.Slides;
        if (from > slides.Count)
            throw new ArgumentException("Invalid source slide position.", nameof(from));

        var cloned = slides[from - 1].Clone();

        var insertAt = to - 1; // 0-based target index
        int newIndex;

        if (insertAt <= 0 || insertAt >= slides.Count)
        {
            slides.Add(cloned);
            newIndex = slides.Count; // 1-based: last position
        }
        else
        {
            slides.Insert(insertAt, cloned); // Insert at 0-based index
            newIndex = insertAt + 1; // convert 0-based back to 1-based
        }

        Save();
        return new SfSlide(this, cloned, newIndex);
    }

    /// <inheritdoc />
    public bool RemoveSlide(int index)
    {
        if (index <= 0) return false;

        var slides = _core.Value.Slides;
        if (index > slides.Count) return false;

        slides.Remove(slides[index - 1]);
        Save();
        return true;
    }

    /// <inheritdoc />
    public void Save(PresentationExtension? extension = null)
    {
        if (!_core.IsValueCreated) return;

        using var ms = new MemoryStream();
        _core.Value.Save(ms);

        ms.Position = 0;
        _fileStream!.SetLength(0);
        _fileStream.Position = 0;
        ms.CopyTo(_fileStream);
        _fileStream.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_core.IsValueCreated)
            _core.Value.Dispose();
        _fileStream?.Dispose();
    }
}
