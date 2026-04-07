using SlideGenerator.Domain.Slide.Models.Previews;

namespace SlideGenerator.Domain.Slide.Interfaces;

public interface IPreviewable<out T>
    where T : ObjectPreview
{
    T GetPreview();
}