using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Models;

/// <summary>
///     Represents one executable worksheet-slide pair from <see cref="Domain.Tasks.Models.GenerateRequest.Graph"/>.
/// </summary>
/// <param name="Worksheet">Source worksheet to read row data from.</param>
/// <param name="Slide">Template slide descriptor used to build output presentation.</param>
public sealed record WorkingItem(WorksheetIdentifier Worksheet, SlideIdentifier Slide);