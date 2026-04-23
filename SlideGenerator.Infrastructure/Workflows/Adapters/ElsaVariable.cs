using Elsa.Extensions;
using Elsa.Workflows;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     A wrapper that maps an Elsa variable to an Application-layer Variable.
/// </summary>
public sealed class ElsaVariable<T>(ActivityExecutionContext context, string name) : Variable<T> where T : notnull
{
    public override T? Value
    {
        get => context.GetVariable<T>(name);
        set => context.SetVariable(name, value);
    }
}