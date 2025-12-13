using TaoSlideTotNghiep.Application.Base.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Errors;

public record ConfigError : Base.DTOs.Responses.ErrorResponse
{
    public ConfigError(Exception exception) : base(RequestType.Config, exception)
    {
    }
}