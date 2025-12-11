using Microsoft.Extensions.Logging;

namespace TaoSlideTotNghiep.Infrastructure.Services;

/// <summary>
/// Base class for services.
/// </summary>
public abstract class Service(ILogger logger)
{
    protected ILogger Logger { get; } = logger;
}