using Elsa.Extensions;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Workflows.Entities.Activities;
using AppActivity = SlideGenerator.Application.Workflows.Entities.Activities.Activity;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

public sealed class ElsaForEach<T> : ForEach<T> where T : notnull
{
    private const string CurrentValueKey = "CurrentValue";

    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    internal Elsa.Workflows.Activities.ForEach<T> ToElsaActivity(Func<AppActivity, ElsaActivity> converter)
    {
        return new Elsa.Workflows.Activities.ForEach<T>
        {
            Items = new Input<ICollection<T>>(Items),
            Body = new Elsa.Workflows.Activities.Sequence
            {
                Activities =
                {
                    new Elsa.Workflows.Activities.Inline(ctx =>
                    {
                        // Update the Application-layer variable with the current item from Elsa's ForEach
                        CurrentValue.Value = ctx.GetVariable<T>(CurrentValueKey);
                        return ValueTask.CompletedTask;
                    }),
                    converter(Body)
                }
            }
        };
    }
}
