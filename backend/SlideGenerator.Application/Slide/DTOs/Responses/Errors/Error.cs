using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Errors;

public record Error : ErrorResponse
{
    public Error(Exception exception) : base(RequestType.Slide, exception)
    {
    }
}