using Elsa.Extensions;
using Elsa.Workflows;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Infrastructure implementation of <see cref="Variable{T}" /> that wraps an Elsa-native variable.
/// </summary>
/// <typeparam name="T">The type of the variable value.</typeparam>
/// <param name="context">The underlying Elsa <see cref="ActivityExecutionContext" />.</param>
/// <param name="name">The name of the variable in the Elsa context.</param>
public sealed class ElsaVariable<T>(ActivityExecutionContext context, string name) : Variable<T> where T : notnull
{
    /// <summary>
    ///     The underlying Elsa <see cref="ActivityExecutionContext" />.
    /// </summary>
    private readonly ActivityExecutionContext context = context;

    /// <summary>
    ///     The name of the variable in the Elsa context.
    /// </summary>
    private readonly string name = name;

    /// <inheritdoc />
    public override T? Value
    {
        get => context.GetVariable<T>(name);
        set => context.SetVariable(name, value);
    }
}