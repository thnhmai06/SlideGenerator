using SlideGenerator.Document.Slide.Models;

namespace SlideGenerator.Pipeline.Scanning.Models.Slides.Requests;

public record PresentationSummaryRequest(PresentationIdentifier Identifier, bool GetPreview = true);