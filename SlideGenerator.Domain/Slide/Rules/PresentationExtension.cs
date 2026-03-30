namespace SlideGenerator.Domain.Slide.Rules;

public enum PresentationExtension
{
    Potx,
    Pptx,
    Ppsx
}

public static class PresentationExtensionExtensions
{
    extension(PresentationExtension extension)
    {
        public string ToFileExtension()
        {
            return extension switch
            {
                PresentationExtension.Potx => ".potx",
                PresentationExtension.Pptx => ".pptx",
                PresentationExtension.Ppsx => ".ppsx",
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }
    }
}