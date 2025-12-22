using SlideGenerator.Domain.Slide;
using CoreWorkingPresentation = SlideGenerator.Framework.Slide.Models.WorkingPresentation;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

/// <summary>
///     Adapter to convert SlideGenerator.Framework.Slide.Models.WorkingPresentation to
///     Domain.Slide.Interfaces.IWorkingPresentation.
/// </summary>
internal sealed class WorkingPresentationAdapter(CoreWorkingPresentation presentation)
    : IWorkingPresentation
{
    public void Dispose()
    {
        presentation.Dispose();
    }

    public string FilePath => presentation.FilePath;

    public int SlideCount => presentation.SlideCount;

    public void RemoveSlide(int position)
    {
        presentation.RemoveSlide(position);
    }

    public void Save()
    {
        presentation.Save();
    }
}