using SlideGenerator.Application.Modules.Workflows.Models.Logging;

namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>
///     Wraps the body in a try/catch boundary. When an exception occurs it is logged to
///     <see cref="IExecutionContext.Snapshot" /> and swallowed so the enclosing loop can continue.
///     If <see cref="Catch" /> is provided it executes in a child scope after logging.
///     If <see cref="ExceptionVariable" /> is also provided the caught exception is stored in that
///     variable so the catch node can read it via <see cref="IExecutionContext.GetVariable{TVar}" />.
/// </summary>
public class Try<TData> : Activity<TData>
{
    public Try() { }

    /// <param name="body">The primary node to execute inside the try block.</param>
    /// <param name="catch">Optional node to run after an exception is caught and logged.</param>
    /// <param name="exceptionVariable">
    ///     Optional variable key into which the caught exception is stored so <paramref name="catch" /> can inspect it.
    /// </param>
    public Try(Activity<TData> body, Activity<TData>? @catch = null, Handle<Exception>? exceptionVariable = null)
    {
        Body = body;
        Catch = @catch;
        ExceptionVariable = exceptionVariable;
    }

    public Activity<TData> Body { get; init; } = default!;
    public Activity<TData>? Catch { get; init; }
    public Handle<Exception>? ExceptionVariable { get; init; }

    public override async Task ExecuteAsync(IExecutionContext<TData> context)
    {
        try
        {
            await Body.ExecuteAsync(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            context.Snapshot.Logger.AddLog(LogLevel.Error, $"Row failed: {e.GetType().Name}: {e.Message}");
            if (Catch is not null)
            {
                var catchCtx = context.CreateChildScope();
                if (ExceptionVariable is not null) catchCtx.SetVariable(ExceptionVariable, e);
                await Catch.ExecuteAsync(catchCtx).ConfigureAwait(false);
            }
        }
    }
}