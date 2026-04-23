using Elsa.Extensions;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Workflows.Entities.Activities;
using AppActivity = SlideGenerator.Application.Workflows.Entities.Activities.Activity;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

public sealed class ElsaParallelForEach<T> : ParallelForEach<T> where T : notnull
{
    private const string ElsaCurrentValue = "CurrentValue";

    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    internal ElsaActivity ToElsaActivity(Func<AppActivity, ElsaActivity> converter)
    {
        return new Elsa.Workflows.Activities.ParallelForEach<T>
        {
            Items = new Input<object>(Items), 
            Body = new Elsa.Workflows.Activities.Sequence
            {
                Activities =
                {
                    new Elsa.Workflows.Activities.Inline(ctx =>
                    {
                        CurrentValue.Value = ctx.GetVariable<T>(ElsaCurrentValue);
                        return ValueTask.CompletedTask;
                    }),
                    converter(Body)
                }
            }
        };
    }
}
