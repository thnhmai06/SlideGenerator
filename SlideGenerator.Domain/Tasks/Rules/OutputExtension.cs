namespace SlideGenerator.Domain.Tasks.Rules;

public enum OutputExtension
{
    Potx,
    Pptx
}

public static class OutputExtensionExtensions
{
    extension(OutputExtension extension)
    {
        public string ToFileExtension()
        {
            return extension switch
            {
                OutputExtension.Potx => ".potx",
                OutputExtension.Pptx => ".pptx",
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }

        // public PresentationDocumentType ToPresentationDocumentType()
        // {
        //     return extension switch
        //     {
        //         OutputExtension.Potx => PresentationDocumentType.Template,
        //         OutputExtension.Pptx => PresentationDocumentType.Presentation,
        //         _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
        //     };
        // }
    }
}