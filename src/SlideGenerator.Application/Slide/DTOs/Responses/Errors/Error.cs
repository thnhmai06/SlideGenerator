using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Errors;

/// <summary>
///     Error response for slide requests.
/// </summary>
public sealed record Error(string Kind, string Message) : Response("error")
{
    public Error(Exception exception)
        : this(exception.GetType().Name, exception.Message)
    {
    }
}