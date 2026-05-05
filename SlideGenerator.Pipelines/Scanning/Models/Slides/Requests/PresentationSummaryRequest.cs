using SlideGenerator.Documents.Slides.Models;

namespace SlideGenerator.Pipelines.Scanning.Models.Slides.Requests;

public record PresentationSummaryRequest(PresentationIdentifier Identifier, bool GetPreview = true);