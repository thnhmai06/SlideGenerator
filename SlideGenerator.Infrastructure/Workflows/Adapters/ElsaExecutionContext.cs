using Elsa.Extensions;
using Elsa.Workflows;
using IExecutionContext = SlideGenerator.Application.Workflows.Entities.Contexts.IExecutionContext;
using AppVariable = SlideGenerator.Application.Workflows.Entities.Contexts.Variable;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Wraps Elsa's <see cref="ActivityExecutionContext" /> to provide an application-layer <see cref="IExecutionContext" />.
/// </summary>
/// <param name="context">The underlying Elsa <see cref="ActivityExecutionContext" />.</param>
public sealed class ElsaExecutionContext(ActivityExecutionContext context) : IExecutionContext
{
    /// <summary>
    ///     The underlying Elsa <see cref="ActivityExecutionContext" />.
    /// </summary>
    private readonly ActivityExecutionContext context = context;

    /// <inheritdoc />
    public string WorkflowInstanceId => context.WorkflowExecutionContext.Id;

    /// <inheritdoc />
    public IEnumerable<AppVariable> Variables
    {
        get
        {
            var currentContext = context;
            
            while (currentContext != null)
            {
                var contextVariables = 
                    from variable in currentContext.Variables
                    where !string.IsNullOrEmpty(variable.Name)
                    select new ElsaVariable<object>(context, variable.Name) { Name = variable.Name };

                foreach (var variable in contextVariables) 
                    yield return variable;

                currentContext = currentContext.ParentActivityExecutionContext;
            }
        }
    }

    /// <inheritdoc />
    public T? GetVariable<T>(string name) where T : notnull
    {
        return context.GetVariable<T>(name);
    }

    /// <inheritdoc />
    public void SetVariable<T>(string name, T? value) where T : notnull
    {
        context.SetVariable(name, value);
    }

    /// <inheritdoc />
    public bool HasVariable(string name)
    {
        return context.ExpressionExecutionContext.GetVariable(name) != null;
    }

    /// <inheritdoc />
    public void RemoveVariable(string name)
    {
        context.SetVariable(name, null);
    }
}