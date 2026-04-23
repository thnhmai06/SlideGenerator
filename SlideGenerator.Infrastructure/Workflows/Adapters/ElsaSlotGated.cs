using Elsa.Extensions;
using Elsa.Workflows.Memory;
using SlideGenerator.Application.Resources.Abstractions;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using AppActivity = SlideGenerator.Application.Workflows.Entities.Activities.Activity;
using ElsaActivity = Elsa.Workflows.Activity;
using AppExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Adapter for SlotGated activity that converts to an Elsa-native Sequence
///     that handles locking and unlocking automatically.
/// </summary>
public sealed class ElsaSlotGated : SlotGated
{
    public override ValueTask ExecuteAsync(AppExecutionContext context, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    internal ElsaActivity ToElsaActivity(Func<AppActivity, ElsaActivity> converter)
    {
        // Use an Elsa variable to pass the lock handle between inline activities
        var handleVariable = new Variable<IKeyedLockHandle?>
        {
            Name = $"SlotHandle_{Gate}"
        };

        return new Elsa.Workflows.Activities.Sequence
        {
            Variables = { handleVariable },
            Activities =
            {
                // 1. Acquire the slot
                new Elsa.Workflows.Activities.Inline(async ctx =>
                {
                    var locker = ctx.GetRequiredService<IAsyncKeyedLocker<SlotType>>();
                    var handle = await locker.LockAsync(Gate, ctx.CancellationToken).ConfigureAwait(false);
                    handleVariable.Set(ctx, handle);
                }),
                
                // 2. Run the actual body
                converter(Body),
                
                // 3. Release the slot
                new Elsa.Workflows.Activities.Inline(ctx =>
                {
                    var handle = handleVariable.Get(ctx);
                    handle?.Dispose();
                    return ValueTask.CompletedTask;
                })
            }
        };
    }
}
