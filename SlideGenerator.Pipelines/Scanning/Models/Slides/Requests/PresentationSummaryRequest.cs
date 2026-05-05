using SlideGenerator.Pipelines.Generating.Models.Identifiers;

namespace SlideGenerator.Pipelines.Scanning.Models.Slides.Requests;

public record PresentationSummaryRequest(PresentationIdentifier Identifier, bool GetPreview = true);
