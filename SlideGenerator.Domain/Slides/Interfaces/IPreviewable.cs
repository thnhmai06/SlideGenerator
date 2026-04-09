using SlideGenerator.Domain.Slides.Models.Previews;

namespace SlideGenerator.Domain.Slides.Interfaces;

public interface IPreviewable<out T>
    where T : ObjectPreview
{
    T GetPreview();
}