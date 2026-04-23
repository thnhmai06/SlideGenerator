using Elsa.Workflows;
using SlideGenerator.Application.Workflows.Entities.Activities;
using AppActivity = SlideGenerator.Application.Workflows.Entities.Activities.Activity;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Infrastructure implementation of <see cref="Sequence" /> that converts to an Elsa-native <see cref="Elsa.Workflows.Activities.Sequence" />.
/// </summary>
public sealed class ElsaSequence : Sequence
{
    /// <inheritdoc />
    /// <remarks>
    ///     This method is not supported because the activity is designed to be converted to an Elsa-native equivalent via <see cref="ToElsaActivity" />.
    /// </remarks>
    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>
    ///     Converts this activity to its Elsa-native equivalent.
    /// </summary>
    /// <param name="converter">The activity converter used to transform child activities.</param>
    /// <returns>An Elsa-native <see cref="Elsa.Workflows.Activities.Sequence" /> activity.</returns>
    internal ElsaActivity ToElsaActivity(Func<AppActivity, ElsaActivity> converter)
    {
        return new Elsa.Workflows.Activities.Sequence
            { Activities = Activities.Select(converter).Cast<IActivity>().ToList() };
    }
}
