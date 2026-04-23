using SlideGenerator.Application.Workflows.Entities.Activities;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Infrastructure implementation of <see cref="Inline" /> that converts to an Elsa-native
///     <see cref="Elsa.Workflows.Activities.Inline" />.
/// </summary>
public sealed class ElsaInline : Inline
{
    /// <inheritdoc />
    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Function(context, cancellationToken);
    }

    /// <summary>
    ///     Converts this activity to its Elsa-native equivalent.
    /// </summary>
    /// <returns>An Elsa-native <see cref="Elsa.Workflows.Activities.Inline" /> activity.</returns>
    internal ElsaActivity ToElsaActivity()
    {
        return new Elsa.Workflows.Activities.Inline(ctx =>
        {
            var executionContext = new ElsaExecutionContext(ctx);
            return ExecuteAsync(executionContext, ctx.CancellationToken);
        });
    }
}