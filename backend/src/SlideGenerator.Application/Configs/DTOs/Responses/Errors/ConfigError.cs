using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Errors;

/// <summary>
///     Error response for configuration operations.
/// </summary>
public sealed record ConfigError(string Kind, string Message) : Response("error")
{
    public ConfigError(Exception exception)
        : this(exception.GetType().Name, exception.Message)
    {
    }
}