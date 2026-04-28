using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Application.Modules.Workflows.Entities.Contexts;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

internal sealed class WcExecutionContext(IServiceProvider services) : IExecutionContext
{
    private readonly Dictionary<string, object?> _variables = new();

    public T GetRequiredService<T>() where T : notnull
    {
        return services.GetRequiredService<T>();
    }

    public T? GetVariable<T>(string name)
    {
        return _variables.TryGetValue(name, out var v) ? (T?)v : default;
    }

    public void SetVariable<T>(string name, T value)
    {
        _variables[name] = value;
    }
}