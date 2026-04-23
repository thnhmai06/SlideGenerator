using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

/// <summary>
///     Represents the base instruction for an image replacement.
/// </summary>
/// <param name="Target">The target shape to replace.</param>
/// <param name="Edit">The editing options for the image.</param>
public abstract record Instruction(ShapeIdentifier Target, EditOptions Edit);