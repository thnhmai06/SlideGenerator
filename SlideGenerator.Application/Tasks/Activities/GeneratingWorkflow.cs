using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Tasks.Models;

namespace SlideGenerator.Application.Tasks.Activities;

public class GeneratingWorkflow : WorkflowBase
{
    public Input<GenerateRequest> Request { get; set; } = null!;
    
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            // TODO: build workflow activities here
        };
    }
}