namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>
///     Executes an inline delegate as a workflow step.
///     Use for lightweight state mutations that do not warrant a dedicated activity class.
///     The lambda receives a typed <see cref="IExecutionContext{TData}" /> for direct <c>ctx.Data</c> access.
/// </summary>
/// <typeparam name="TData">The workflow data type.</typeparam>
public class Inline<TData> : Activity<TData>
{
    public Inline() { }

    public Inline(Func<IExecutionContext<TData>, Task> action)
    {
        Action = action;
    }

    public Func<IExecutionContext<TData>, Task> Action { get; init; } = default!;

    /// <summary>
    ///     Creates an <see cref="Inline{TData}" /> node that executes a leaf activity resolved from DI.
    /// </summary>
    /// <typeparam name="TActivity">The activity type to execute.</typeparam>
    /// <param name="args">Optional arguments to pass to the activity constructor if manually instantiating.</param>
    public static Inline<TData> Activity<TActivity>(params object[] args)
    {
        return new Inline<TData>
        {
            Action = async ctx =>
            {
                // Simple Activator-based resolution for now. 
                // In a real scenario, this would use ActivatorUtilities.
                var activity = (TActivity)ctx.Services.GetService(typeof(TActivity))!
                               ?? throw new InvalidOperationException($"Activity {typeof(TActivity).Name} not registered.");

                var method = typeof(TActivity).GetMethod("ExecuteAsync")
                             ?? throw new InvalidOperationException("ExecuteAsync method not found.");

                await ((Task)method.Invoke(activity, [ctx])!).ConfigureAwait(false);
            }
        };
    }
    
    public override async Task ExecuteAsync(IExecutionContext<TData> context)
    {
        await Action(context).ConfigureAwait(false);
    }
}