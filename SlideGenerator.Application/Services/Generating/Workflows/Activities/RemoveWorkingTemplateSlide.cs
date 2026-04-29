using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Models.States;
using SlideGenerator.Application.Services.Generating.Workflows.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Removes the working template slide (index 1) from the presentation once all row slides have
///     been generated, then saves and closes the file.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorkingTemplateSlide" />.<br />
///     <b>State:</b> Reads and disposes <see cref="WorksheetContext.PresentationLease" />.<br />
///     <b>Data read:</b> <see cref="WorkflowTask.Request" /> (<c>OutputExtension</c>).
/// </remarks>
public sealed class RemoveWorkingTemplateSlide : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var slideIdentifier = context.GetVariable(VariablesDeclaration.WorkingTemplateSlide)
                              ?? throw new ArgumentException(
                                  "Working template slide identifier must be set in context.");

        var worksheetSnapshot = CloneTemplateSlide.GetWorksheetSnapshot(context);
        var lease = worksheetSnapshot.Context.PresentationLease
                    ?? throw new InvalidOperationException(
                        "Presentation lease must be open before removing template slide.");

        var presentation = lease.Value;
        presentation.RemoveSlide(slideIdentifier.Index);
        presentation.Save(context.Data.Request.OutputExtension);

        lease.Dispose();
        worksheetSnapshot.Context.PresentationLease = null;

        return Task.CompletedTask;
    }
}