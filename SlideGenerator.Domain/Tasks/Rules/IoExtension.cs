using SlideGenerator.Domain.Slide.Rules;

namespace SlideGenerator.Domain.Tasks.Rules;

public static class IoExtension
{
    public static IReadOnlyList<PresentationExtension> InputPresentation { get; } = [PresentationExtension.Pptx, PresentationExtension.Potx];

    public static IReadOnlyList<PresentationExtension> OutputPresentation { get; } = [PresentationExtension.Pptx, PresentationExtension.Ppsx];
}