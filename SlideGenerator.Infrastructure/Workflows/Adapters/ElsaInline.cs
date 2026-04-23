using SlideGenerator.Application.Workflows.Entities.Activities;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

public sealed class ElsaInline : Inline
{
    /// <inheritdoc />
    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default) =>
        Function(context, cancellationToken);

    internal ElsaActivity ToElsaActivity()
    {
        return new Elsa.Workflows.Activities.Inline(ctx => 
        {
            var executionContext = new ElsaExecutionContext(ctx);
            return ExecuteAsync(executionContext, ctx.CancellationToken);
        });
    }
}
