namespace SlideGenerator.Domain.Slide.Models.Previews;

/// <param name="Id">The id of Slide in Presentation.</param>
public record SlidePreview(int Index, uint Id, string Name, byte[] Image) : ObjectPreview(Name, Image);