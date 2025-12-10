using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Base class for services.
/// </summary>
public abstract class Service(ILogger logger)
{
    protected ILogger Logger { get; } = logger;
}