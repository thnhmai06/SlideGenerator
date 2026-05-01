/* LEGACY-OPENXML — replaced by SfPresentationRegistry (Syncfusion.Presentation.NET)
using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Infrastructure.Slides.Adapters;

namespace SlideGenerator.Infrastructure.Slides.Services;

public sealed class XmlPresentationRegistry(FileLocker locker)
    : FileRegistry<IPresentation>(locker)
{
    protected override IPresentation CreateInstance(string normalizedPath, bool isEditable)
    {
        return new XmlPresentation(normalizedPath, isEditable);
    }
}
*/
