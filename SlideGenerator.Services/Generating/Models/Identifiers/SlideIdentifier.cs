namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record SlideIdentifier(string PresentationFilePath, uint SlideIndex, string? PresentationPassword = null)
    : PresentationIdentifier(PresentationFilePath, PresentationPassword)
{
    public uint SlideIndex
    {
        get;
        init => field = Math.Max(1, value);
    } = SlideIndex;
}