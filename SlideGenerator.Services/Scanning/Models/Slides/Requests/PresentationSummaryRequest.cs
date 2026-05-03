using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Scanning.Models.Slides.Requests;

public record PresentationSummaryRequest(PresentationIdentifier Identifier, bool GetPreview = true);