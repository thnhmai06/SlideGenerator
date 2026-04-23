using Elsa.Extensions;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Workflows.Entities.Activities;
using AppActivity = SlideGenerator.Application.Workflows.Entities.Activities.Activity;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Infrastructure implementation of <see cref="ForEach{T}" /> that converts to an Elsa-native <see cref="Elsa.Workflows.Activities.ForEach{T}" />.
/// </summary>
/// <typeparam name="T">The type of items to iterate over.</typeparam>
public sealed class ElsaForEach<T> : ForEach<T> where T : notnull
{
    /// <summary>
    ///     The key used by Elsa to store the current item in the iteration.
    /// </summary>
    private const string CurrentValueKey = "CurrentValue";

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
    /// <returns>An Elsa-native <see cref="Elsa.Workflows.Activities.ForEach{T}" />.</returns>
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
