using SlideGenerator.Application.Base.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Errors;

public record Error : Base.DTOs.Responses.ErrorResponse
{
    public Error(Exception exception) : base(RequestType.Slide, exception)
    {
    }
}