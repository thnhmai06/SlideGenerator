using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Application.Slide.Contracts;

/// <summary>
///     Interface for generating presentation service.
/// </summary>
public interface ISlideGeneratingService
{
    bool AddDerivedPresentation(string filepath, string sourcePath);
    bool RemoveDerivedPresentation(string filepath);
    IDerivedPresentation GetDerivedPresentation(string filepath);
}