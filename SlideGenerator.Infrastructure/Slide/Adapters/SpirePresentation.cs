using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;
using SlideGenerator.Domain.Slide.Rules;
using Spire.Presentation;
using ISlide = SlideGenerator.Domain.Slide.Entities.ISlide;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class SpirePresentation : IPresentation, IDisposable
{
    internal readonly Lazy<Presentation> Core;
    private readonly Lazy<FileStream> _coreFs;

    public SpirePresentation(string filePath, bool isEditable = true)
    {
        FilePath = filePath;
        IsEditable = isEditable;
        _coreFs = new Lazy<FileStream>(() => new FileStream(filePath, FileMode.Open,
                isEditable ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite),
            LazyThreadSafetyMode.ExecutionAndPublication);
        Core = new Lazy<Presentation>(() => new Presentation(_coreFs.Value, FileFormat.Auto),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string FilePath { get; }

    public bool IsEditable { get; }

    public IEnumerable<ISlide> EnumerateSlides()
    {
        foreach (var (slide, index) in Core.Value.Slides.ToArray().Select((s, i) => (s, i + 1)))
        {
            yield return new SpireSlide(slide) { Index = index };
        }
    }

    public void Save(PresentationExtension? extension)
    {
        if (_coreFs.IsValueCreated)
            _coreFs.Value.Position = 0;
        if (Core.IsValueCreated)
            Core.Value.SaveToFile(_coreFs.Value, extension.ToFileFormat());
    }

    public void Dispose()
    {
        if (Core.IsValueCreated)
            Core.Value.Dispose();
        if (_coreFs.IsValueCreated)
            _coreFs.Value.Dispose();
    }
}