using Microsoft.Extensions.Logging;

namespace SlideGenerator.Infrastructure.Common.Base;

/// <summary>
///     Base class for services.
/// </summary>
public abstract class Service(ILogger logger)
{
    protected ILogger Logger { get; } = logger;
}