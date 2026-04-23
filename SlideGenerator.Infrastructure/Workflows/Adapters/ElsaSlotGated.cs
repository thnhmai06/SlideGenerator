using Elsa.Extensions;
using Elsa.Workflows.Memory;
using SlideGenerator.Application.Resources.Abstractions;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using AppActivity = SlideGenerator.Application.Workflows.Entities.Activities.Activity;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;
using Inline = Elsa.Workflows.Activities.Inline;
using Sequence = Elsa.Workflows.Activities.Sequence;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Infrastructure implementation of <see cref="SlotGated" /> that converts to an Elsa-native
///     <see cref="Elsa.Workflows.Activities.Sequence" />.
/// </summary>
/// <remarks>
///     The resulting sequence handles acquiring and releasing a lock via <see cref="IAsyncKeyedLocker{TKey}" />
///     automatically.
/// </remarks>
public sealed class ElsaSlotGated : SlotGated
{
    /// <inheritdoc />
    /// <remarks>
    ///     This method is not supported because the activity is designed to be converted to an Elsa-native equivalent via
    ///     <see cref="ToElsaActivity" />.
    /// </remarks>
    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Converts this activity to its Elsa-native equivalent.
    /// </summary>
    /// <param name="converter">The activity converter used to transform child activities.</param>
    /// <returns>An Elsa-native <see cref="Elsa.Workflows.Activities.Sequence" /> that manages the gate lifecycle.</returns>
    internal ElsaActivity ToElsaActivity(Func<AppActivity, ElsaActivity> converter)
    {
        // Use an Elsa variable to pass the lock handle between inline activities
        var handleVariable = new Variable<IKeyedLockHandle?>
        {
            Name = $"SlotHandle_{Gate}"
        };

        return new Sequence
        {
            Variables = { handleVariable },
            Activities =
            {
                // 1. Acquire the slot
                new Inline(async ctx =>
                {
                    var locker = ctx.GetRequiredService<IAsyncKeyedLocker<SlotType>>();
                    var handle = await locker.LockAsync(Gate, ctx.CancellationToken).ConfigureAwait(false);
                    handleVariable.Set(ctx, handle);
                }),

                // 2. Run the actual body
                converter(Body),

                // 3. Release the slot
                new Inline(ctx =>
                {
                    var handle = handleVariable.Get(ctx);
                    handle?.Dispose();
                    return ValueTask.CompletedTask;
                })
            }
        };
    }
}