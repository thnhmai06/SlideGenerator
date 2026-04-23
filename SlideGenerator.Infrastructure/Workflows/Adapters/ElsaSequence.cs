using Elsa.Workflows;
using SlideGenerator.Application.Workflows.Entities.Activities;
using AppActivity = SlideGenerator.Application.Workflows.Entities.Activities.Activity;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

public sealed class ElsaSequence : Sequence
{
    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    internal ElsaActivity ToElsaActivity(Func<AppActivity, ElsaActivity> converter)
    {
        return new Elsa.Workflows.Activities.Sequence
            { Activities = Activities.Select(converter).Cast<IActivity>().ToList() };
    }
}
