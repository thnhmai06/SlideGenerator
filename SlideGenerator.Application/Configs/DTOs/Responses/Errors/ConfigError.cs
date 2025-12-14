using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Errors;

public record ConfigError : ErrorResponse
{
    public ConfigError(Exception exception) : base(RequestType.Config, exception)
    {
    }
}