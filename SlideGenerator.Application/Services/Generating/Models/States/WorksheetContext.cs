using SlideGenerator.Application.Modules.Resources.Entities;
using SlideGenerator.Application.Modules.Workflows.Models.States;
using SlideGenerator.Domain.Slides.Entities.Presentation;

namespace SlideGenerator.Application.Services.Generating.Models.States;

/// <summary>
///     Transient execution state for a worksheet processing scope.
///     Holds resources that are live only while the worksheet is being processed.
/// </summary>
public sealed class WorksheetContext : IExecutionContext
{
    /// <summary>
    ///     The open write lease on the working presentation for Phase B.
    ///     Acquired lazily by <c>CloneTemplateSlide</c>; disposed and reset to
    ///     <see langword="null" /> by <c>RemoveWorkingTemplateSlide</c>.
    /// </summary>
    public Lease<IPresentation>? PresentationLease { get; set; }
}
