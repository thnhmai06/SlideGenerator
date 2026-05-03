namespace SlideGenerator.Services.Scanning.Models.Slides.Requests;

public record PresentationSummaryRequest(string FilePath, string? Password = null, bool GetPreview = true);