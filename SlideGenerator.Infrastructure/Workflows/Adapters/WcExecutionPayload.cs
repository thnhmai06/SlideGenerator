using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

internal sealed class WcExecutionPayload : IExecutionPayload
{
    private readonly Dictionary<string, object?> _variables = new();

    public T? GetVariable<T>(string name)
    {
        return _variables.TryGetValue(name, out var v) ? (T?)v : default;
    }

    public void SetVariable<T>(string name, T value)
    {
        _variables[name] = value;
    }
}